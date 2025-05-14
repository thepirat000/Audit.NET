using System;

namespace Audit.IntegrationTest
{
    public static class AzureSettings
    {
        public static string AzureBlobAccountName => GetFromEnv("AZUREBLOBACCOUNTNAME");
        public static string AzureBlobAccountKey => GetFromEnv("AZUREBLOBACCOUNTKEY");
        public static string AzureBlobCnnString => GetFromEnv("AZUREBLOBCNNSTRING");
        public static string AzureTableCnnString => GetFromEnv("AZURETABLECNNSTRING");
        public static string AzureDocDbUrl => GetFromEnv("AZUREDOCDBURL");
        public static string AzureDocDbAuthKey => GetFromEnv("AZUREDOCDBAUTHKEY");
        public static string AzureEventHubCnnString => GetFromEnv("AZUREEVTHUBCNNSTRING");

        public static string ElasticSearchUrl => "http://elastic:elastic@127.0.0.1:9200";
        public static string OpenSearchUrl => "http://admin:Messi1708!!!@127.0.0.1:9250";
        public static string PostgreSqlConnectionString => "Server=localhost;Port=5432;User Id=postgres;Password=admin;Database=postgres;";

        private static string GetFromEnv(string key, string @default = null)
        {
            var varName = "AUDIT_NET_" + key.ToUpper();
            var value = Environment.GetEnvironmentVariable(varName) ?? @default ?? throw new Exception($"No environment variable or default set for variable '{key}'");
            return value;
        }
    }
}