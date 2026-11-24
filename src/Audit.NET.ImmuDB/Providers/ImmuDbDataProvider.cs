using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;

using Google.Protobuf.WellKnownTypes;

using ImmuDB;

using ImmudbProxy;

// ReSharper disable EmptyGeneralCatchClause

namespace Audit.ImmuDB.Providers;

/// <summary>
/// ImmuDB data provider
/// </summary>
/// <remarks>
/// Settings:
/// <list type="bullet">
/// <item><b>ClientBuilder</b>: <c>Action&lt;ImmuClientBuilder&gt;</c> to configure the ImmuDB client.</item>
/// <item><b>DatabaseName</b>: <c>Setting&lt;string&gt;</c> to specify the database name if not set by the client builder. Default is <c>"defaultdb"</c>.</item>
/// <item><b>Username</b>: <c>Setting&lt;string&gt;</c> to specify the username if not set by the client builder. Default is <c>"immudb"</c>.</item>
/// <item><b>Password</b>: <c>Setting&lt;string&gt;</c> to specify the password if not set by the client builder. Default is <c>"immudb"</c>.</item>
/// <item><b>KeySelector</b>: <c>Func&lt;AuditEvent, byte[]&gt;</c> to select the key for the event. Default is a new GUID converted to a byte array.</item>
/// <item><b>ValueSelector</b>: <c>Func&lt;AuditEvent, byte[]&gt;</c> to select the value for the event. Default is the event serialized to JSON and converted to a byte array.</item>
/// <item><b>UseVerifiedMethods</b>: <c>bool</c> indicating whether to use verified methods for setting values. Default is <c>false</c>.</item>
/// <item><b>ExpirationTimeout</b>: <c>TimeSpan</c> indicating the expiration timeout for the events stored in ImmuDB. If not set or set to NULL, events will not expire.</item>
/// <item><b>MaxConcurrentWrites</b>: <c>int</c> maximum number of non-verified writes in flight at once per ImmuDB connection. Default is 16.</item>
/// </list>
/// </remarks>
public class ImmuDbDataProvider : AuditDataProvider
{
    /// <summary>
    /// Default value for <see cref="MaxConcurrentWrites"/>.
    /// </summary>
    public const int DefaultMaxConcurrentWrites = 16;

    // One client per server/database/credentials. Lazy ensures that concurrent first calls open a single client (and a single server session).
    private static readonly ConcurrentDictionary<uint, Lazy<Task<ImmuClient>>> ClientCache = new();
    // VerifiedSet reads and updates the client's local verified state, so verified writes on one client must not overlap.
    private static readonly ConcurrentDictionary<uint, SemaphoreSlim> VerifiedLocks = new();
    // Set and ExpirableSet are stateless calls multiplexed over the client's gRPC channel; they are only bounded so the server's concurrent-write limit is not exceeded.
    private static readonly ConcurrentDictionary<uint, SemaphoreSlim> WriteSlots = new();
    
    /// <summary>
    /// Action to configure the ImmuDB client.
    /// </summary>
    public Action<ImmuClientBuilder> ClientBuilderAction { get; set; }

    /// <summary>
    /// The name of the database to use when connecting to the ImmuDB server. If not specified, the value defined in the client builder is used, which defaults to "defaultdb".
    /// </summary>
    public Setting<string> DatabaseName { get; set; }

    /// <summary>
    /// The username to use when connecting to the ImmuDB server. If not specified, the value defined in the client builder is used, which defaults to "immudb".
    /// </summary>
    public Setting<string> Username { get; set; }

    /// <summary>
    /// The password to use when connecting to the ImmuDB server. If not specified, the value defined in the client builder is used, which defaults to "immudb".
    /// </summary>
    public Setting<string> Password { internal get; set; }

    /// <summary>
    /// The expiration timeout for the events stored in ImmuDB. If not set or set to NULL, events will not expire.
    /// </summary>
    public Setting<TimeSpan?> ExpirationTimeout { get; set; }

    /// <summary>
    /// Function to select the key for the event. If not set, defaults to a new GUID converted to a byte array.
    /// </summary>
    public Func<AuditEvent, byte[]> KeySelector { get; set; }

    /// <summary>
    /// Function to select the value for the event. If not set, defaults to the event serialized to UTF-8 JSON.
    /// </summary>
    public Func<AuditEvent, byte[]> ValueSelector { get; set; }

    /// <summary>
    /// Indicates whether to use verified methods for setting values in ImmuDB. If true, uses `VerifiedSet`; otherwise, uses `Set`.
    /// </summary>
    public bool UseVerifiedMethods { get; set; }

    /// <summary>
    /// Maximum number of non-verified writes (<c>Set</c> / <c>ExpirableSet</c>) in flight at once per ImmuDB connection (server, database and credentials).
    /// The limit is shared by all the providers using the same connection, and is taken from the first one that writes to it.
    /// Verified writes are always executed one at a time per connection. Default is 16.
    /// </summary>
    public int MaxConcurrentWrites { get; set; } = DefaultMaxConcurrentWrites;
    
    static ImmuDbDataProvider()
    {
        // Register the ImmuDB SDK resources release on process exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                ImmuClient.ReleaseSdkResources();
            }
            catch
            {
                // Ignore any exceptions during the release of resources
            }
        };
    }

    /// <summary>
    /// Default constructor for ImmuDbDataProvider.
    /// </summary>
    public ImmuDbDataProvider()
    {
    }

    /// <summary>
    /// Constructor for ImmuDbDataProvider that accepts a configuration action to set up the provider settings.
    /// </summary>
    /// <param name="config">The configuration action to set up the provider settings.</param>
    public ImmuDbDataProvider(Action<ConfigurationApi.IImmuDbProviderConfigurator> config)
    {
        if (config != null)
        {
            var immuConfig = new ConfigurationApi.ImmuDbProviderConfigurator();
            config.Invoke(immuConfig);

            ClientBuilderAction = immuConfig._clientBuilderAction;
            DatabaseName = immuConfig._databaseName;
            Username = immuConfig._username;
            Password = immuConfig._password;
            KeySelector = immuConfig._keySelector;
            ValueSelector = immuConfig._valueSelector;
            UseVerifiedMethods = immuConfig._useVerifiedMethods;
            ExpirationTimeout = immuConfig._timeout;
            if (immuConfig._maxConcurrentWrites.HasValue)
            {
                MaxConcurrentWrites = immuConfig._maxConcurrentWrites.Value;
            }
        }
    }

    public override object InsertEvent(AuditEvent auditEvent)
    {
        return InsertEventAsync(auditEvent).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var key = KeySelector?.Invoke(auditEvent) ?? Utils.ToByteArray(Guid.NewGuid().ToString());

        await SafeSetValueAsync(auditEvent, key, cancellationToken);
        
        return key;
    }

    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        ReplaceEventAsync(eventId, auditEvent).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var key = (byte[])eventId;

        await SafeSetValueAsync(auditEvent, key, cancellationToken);
    }

    /// <summary>
    /// Gets or creates an ImmuClient instance based on the audit event's settings.
    /// </summary>
    public async Task<ImmuClient> GetClientAsync(AuditEvent auditEvent)
    {
        var (_, client) = await GetOrCreateClientAsync(auditEvent);

        return client;
    }

    private async Task SafeSetValueAsync(AuditEvent auditEvent, byte[] key, CancellationToken cancellationToken)
    {
        var (hash, client) = await GetOrCreateClientAsync(auditEvent);

        var value = ValueSelector?.Invoke(auditEvent) ?? Utils.ToByteArray(auditEvent.ToJson());
        var expiration = ExpirationTimeout.GetValue(auditEvent);

        var verified = !expiration.HasValue && UseVerifiedMethods;

        var semaphore = verified
            ? VerifiedLocks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1))
            : WriteSlots.GetOrAdd(hash, _ => new SemaphoreSlim(Math.Max(1, MaxConcurrentWrites)));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (expiration.HasValue)
            {
                await client.ExpirableSet(key, value, DateTime.Now.Add(expiration.Value));
            }
            else if (verified)
            {
                await client.VerifiedSet(key, value);
            }
            else
            {
                await client.Set(key, value);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Resets the client cache, closing all ImmuClient instances and clearing the cache.
    /// </summary>
    internal static async Task ResetClientCacheAsync()
    {
        foreach (var lazyClient in ClientCache.Values)
        {
            if (lazyClient.IsValueCreated && lazyClient.Value.Status == TaskStatus.RanToCompletion)
            {
                await lazyClient.Value.Result.Close();
            }
        }
        ClientCache.Clear();
        VerifiedLocks.Clear();
        WriteSlots.Clear();
    }

    private async Task<(uint Hash, ImmuClient Client)> GetOrCreateClientAsync(AuditEvent auditEvent)
    {
        var builder = ImmuClient.NewBuilder();

        ClientBuilderAction?.Invoke(builder);

        var database = DatabaseName.GetValue(auditEvent) ?? builder.Database;
        var username = Username.GetValue(auditEvent) ?? builder.Username;
        var password = Password.GetValue(auditEvent) ?? builder.Password;

        // The server address is part of the key: providers pointing to different servers must not share a client
        var hash = Fnv1AHash($"{builder.ServerUrl}|{builder.ServerPort}|{database}|{username}|{password}");

        var lazyClient = ClientCache.GetOrAdd(hash, _ => new Lazy<Task<ImmuClient>>(() => OpenClientAsync(builder, username, password, database)));

        try
        {
            return (hash, await lazyClient.Value);
        }
        catch
        {
            // Do not cache a failed connection attempt, so the next call retries
            ((ICollection<KeyValuePair<uint, Lazy<Task<ImmuClient>>>>)ClientCache).Remove(new KeyValuePair<uint, Lazy<Task<ImmuClient>>>(hash, lazyClient));
            throw;
        }
    }

    private static async Task<ImmuClient> OpenClientAsync(ImmuClientBuilder builder, string username, string password, string database)
    {
        var client = builder.Build();

        await client.Open(username, password, database);

        return client;
    }

    /// <summary>
    /// Computes a Fowler-Noll-Vo 1a hash for the given input string.
    /// </summary>
    private static uint Fnv1AHash(string input)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in input)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash;
        }
    }
}
