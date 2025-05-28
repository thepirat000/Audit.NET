using System;
using System.Collections.Concurrent;
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
/// </list>
/// </remarks>
public class ImmuDbDataProvider : AuditDataProvider
{
    private static readonly ConcurrentDictionary<uint, ImmuClient> ClientCache = new();
    private static readonly ConcurrentDictionary<uint, SemaphoreSlim> ClientLocks = new();
    
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

        var semaphore = ClientLocks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (expiration.HasValue)
            {
                await client.ExpirableSet(key, value, DateTime.Now.Add(expiration.Value));
            }
            else if (UseVerifiedMethods)
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
        foreach (var client in ClientCache.Values)
        {
            await client.Close();
        }
        ClientCache.Clear();
        ClientLocks.Clear();
    }

    private async Task<(uint Hash, ImmuClient Client)> GetOrCreateClientAsync(AuditEvent auditEvent)
    {
        var builder = ImmuClient.NewBuilder();

        ClientBuilderAction?.Invoke(builder);

        var database = DatabaseName.GetValue(auditEvent) ?? builder.Database;
        var username = Username.GetValue(auditEvent) ?? builder.Username;
        var password = Password.GetValue(auditEvent) ?? builder.Password;

        var hash = Fnv1AHash($"{database}|{username}|{password}");

        if (ClientCache.TryGetValue(hash, out var client))
        {
            return (hash, client);
        }

        client = builder.Build();

        await client.Open(username, password, database);
        
        ClientCache[hash] = client;
        ClientLocks[hash] = new SemaphoreSlim(1, 1);

        return (hash, client);
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
