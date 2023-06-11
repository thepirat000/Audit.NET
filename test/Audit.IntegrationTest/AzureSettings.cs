using System;

namespace Audit.IntegrationTest
{
    public static class AzureSettings
    {
        public static readonly string AzureBlobServiceUrl = GetFromEnv("AZUREBLOBSERVICEURL");
        public static readonly string AzureBlobAccountName = GetFromEnv("AZUREBLOBACCOUNTNAME");
        public static readonly string AzureBlobAccountKey = GetFromEnv("AZUREBLOBACCOUNTKEY");
        public static readonly string AzureBlobCnnString = GetFromEnv("AZUREBLOBCNNSTRING");
        public static readonly string AzureTableCnnString = GetFromEnv("AZURETABLECNNSTRING");
        public static readonly string AzureDocDbUrl = GetFromEnv("AZUREDOCDBURL");
        public static readonly string AzureDocDbAuthKey = GetFromEnv("AZUREDOCDBAUTHKEY");
        public static readonly string BlobAccountName = GetFromEnv("BLOBACCOUNTNAME");
        public static readonly string BlobTenantId = GetFromEnv("BLOBTENANTID");
        
        public static readonly string ElasticSearchUrl = "http://127.0.0.1:9200";
        public static readonly string PostgreSqlConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=admin;Database=postgres;";

        private static string GetFromEnv(string key, string @default = null)
        {
            var varName = "AUDIT_NET_" + key.ToUpper();
            var value = Environment.GetEnvironmentVariable(varName) ?? @default ?? throw new Exception($"No environment variable or default set for variable '{key}'");
            return value;
        }
    }
}