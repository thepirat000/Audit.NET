using ImmuDB;
using System;
using Audit.Core;

namespace Audit.ImmuDB.ConfigurationApi
{
    public interface IImmuDbProviderConfigurator
    {
        /// <summary>
        /// Configures the ImmuDB client builder with the specified action.
        /// </summary>
        /// <param name="clientBuilderAction">The action to configure the ImmuDB client builder.</param>
        IImmuDbProviderConfigurator ClientBuilder(Action<ImmuClientBuilder> clientBuilderAction);

        /// <summary>
        /// Sets the database name for the ImmuDB provider.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        IImmuDbProviderConfigurator Database(string databaseName);

        /// <summary>
        /// Sets the database name for the ImmuDB provider using a function that takes an AuditEvent and returns a string. If not specified, the value defined in the client builder is used, which defaults to "defaultdb".
        /// </summary>
        /// <param name="databaseNameBuilder">The function to build the database name based on the AuditEvent.</param>
        IImmuDbProviderConfigurator Database(Func<AuditEvent, string> databaseNameBuilder);

        /// <summary>
        /// Sets the username for the ImmuDB provider.
        /// </summary>
        /// <param name="username">The username to use when connecting to the ImmuDB server. If not specified, the value defined in the client builder is used, which defaults to "immudb".</param>
        IImmuDbProviderConfigurator Username(string username);

        /// <summary>
        /// Sets the username for the ImmuDB provider using a function that takes an AuditEvent and returns a string. If not specified, the value defined in the client builder is used, which defaults to "immudb".
        /// </summary>
        /// <param name="usernameBuilder"></param>
        IImmuDbProviderConfigurator Username(Func<AuditEvent, string> usernameBuilder);

        /// <summary>
        /// Sets the password for the ImmuDB provider.
        /// </summary>
        /// <param name="password">The password to use when connecting to the ImmuDB server. If not specified, the value defined in the client builder is used, which defaults to "immudb".</param>
        IImmuDbProviderConfigurator Password(string password);

        /// <summary>
        /// Sets the password for the ImmuDB provider using a function that takes an AuditEvent and returns a string. If not specified, the value defined in the client builder is used, which defaults to "immudb".
        /// </summary>
        /// <param name="passwordBuilder">The function to build the password based on the AuditEvent.</param>
        IImmuDbProviderConfigurator Password(Func<AuditEvent, string> passwordBuilder);

        /// <summary>
        /// Sets the key selector for the ImmuDB provider.
        /// </summary>
        /// <param name="keySelector">The function to select the key for the event. If not set, defaults to a new GUID converted to a byte array.</param>
        IImmuDbProviderConfigurator KeySelector(Func<AuditEvent, byte[]> keySelector);

        /// <summary>
        /// Sets the key selector for the ImmuDB provider using a function that takes an AuditEvent and returns a string. If not set, defaults to a new GUID.
        /// </summary>
        /// <param name="keySelector">The function to select the key for the event as a string.</param>
        /// <returns></returns>
        IImmuDbProviderConfigurator KeySelector(Func<AuditEvent, string> keySelector);

        /// <summary>
        /// Sets the value selector for the ImmuDB provider.
        /// </summary>
        /// <param name="valueSelector">The function to select the value for the event. If not set, defaults to the event serialized to UTF-8 JSON.</param>
        IImmuDbProviderConfigurator ValueSelector(Func<AuditEvent, byte[]> valueSelector);

        /// <summary>
        /// Sets the value selector for the ImmuDB provider using a function that takes an AuditEvent and returns a string. If not set, defaults to the event serialized to UTF-8 JSON.
        /// </summary>
        /// <param name="valueSelector">The function to select the value for the event as a string.</param>
        /// <returns></returns>
        IImmuDbProviderConfigurator ValueSelector(Func<AuditEvent, string> valueSelector);

        /// <summary>
        /// Sets whether to use verified methods for setting values in ImmuDB. If true, uses `VerifiedSet`; otherwise, uses `Set`.
        /// </summary>
        /// <param name="useVerifiedMethods">true to use verified methods; otherwise, false.</param>
        IImmuDbProviderConfigurator UseVerifiedMethods(bool useVerifiedMethods = true);

        /// <summary>
        /// Sets the expiration timeout for the events stored in ImmuDB. If not set, events will not expire.
        /// </summary>
        /// <param name="timespan">The expiration timeout for the events stored in ImmuDB.</param>
        /// <returns></returns>
        IImmuDbProviderConfigurator ExpirationTimeout(TimeSpan timespan);

        /// <summary>
        /// Sets the expiration timeout for the events stored in ImmuDB using a function that takes an AuditEvent and returns a TimeSpan. If not set or returns NULL, event will not expire.
        /// </summary>
        /// <param name="timespanBuilder">The function to build the expiration timeout based on the AuditEvent.</param>
        /// <returns></returns>
        IImmuDbProviderConfigurator ExpirationTimeout(Func<AuditEvent, TimeSpan?> timespanBuilder);
    }
}