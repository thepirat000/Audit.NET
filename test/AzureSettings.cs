using System;

namespace Audit.IntegrationTest
{
    public static class AzureSettings
    {
        public static string AzureBlobAccountName => GetFromEnv("AUDIT_NET_AZUREBLOBACCOUNTNAME");
        public static string AzureBlobAccountKey => GetFromEnv("AUDIT_NET_AZUREBLOBACCOUNTKEY");
        public static string AzureBlobCnnString => GetFromEnv("AUDIT_NET_AZUREBLOBCNNSTRING");
        public static string AzureTableCnnString => GetFromEnv("AUDIT_NET_AZURETABLECNNSTRING");
        public static string AzureDocDbUrl => GetFromEnv("AUDIT_NET_AZUREDOCDBURL");
        public static string AzureDocDbAuthKey => GetFromEnv("AUDIT_NET_AZUREDOCDBAUTHKEY");
        public static string AzureEventHubCnnString => GetFromEnv("AUDIT_NET_AZUREEVTHUBCNNSTRING");

        public static string ElasticSearchUrl => "http://elastic:elastic@127.0.0.1:9200";
        public static string OpenSearchUrl => "http://admin:Messi1708!!!@127.0.0.1:9250";
        public static string PostgreSqlConnectionString => "Server=localhost;Port=5432;User Id=postgres;Password=admin;Database=postgres;";

        private static string GetFromEnv(string key, string @default = null)
        {
            var varName = key.ToUpper();
            var value = Environment.GetEnvironmentVariable(varName) ?? @default ?? throw new Exception($"No environment variable or default set for variable '{key}'");
            return value;
        }
    }
}