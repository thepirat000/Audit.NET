using System;

namespace Audit.IntegrationTest
{
    public static class AzureSettings
    {
        public static string AzureBlobAccountName => "devstoreaccount1";
        public static string AzureBlobAccountKey => "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="; // Well-known development storage account key to use with Azurite Emulator
        public static string AzureBlobCnnString => "UseDevelopmentStorage=true";
        public static string AzureTableCnnString => "UseDevelopmentStorage=true";
        public static string AzureDocDbUrl => "http://localhost:8082/";
        public static string AzureDocDbAuthKey => "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public static string AzureEventHubCnnString => "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;EntityPath=eh1;UseDevelopmentEmulator=true;";

        public static string ElasticSearchUrl => "http://elastic:elastic@127.0.0.1:9200";
        public static string OpenSearchUrl => "http://admin:Messi1708!!!@127.0.0.1:9250";
        public static string PostgreSqlConnectionString => "Server=localhost;Port=5432;User Id=postgres;Password=admin;Database=postgres;";
        public static string SqlServerConnectionString => "Server=localhost,1433;Initial Catalog=Audit;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=False;TrustServerCertificate=True;";

        private static string GetFromEnv(string key, string @default = null)
        {
            var varName = key.ToUpper();
            var value = Environment.GetEnvironmentVariable(varName) ?? @default ?? throw new Exception($"No environment variable or default set for variable '{key}'");
            return value;
        }
    }
}