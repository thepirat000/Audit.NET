namespace Audit.IntegrationTest
{
    public static class AzureSettings
    {
        public const string AzureBlobCnnString = "DefaultEndpointsProtocol=https;AccountName=thepirat;AccountKey=XXXXX==;EndpointSuffix=core.windows.net";
        public const string AzureDocDbUrl = "https://XXXXX.documents.azure.com:443/";
        public const string AzureDocDbAuthKey = "XXXXXXX";
        public const string ElasticSearchUrl = "http://localhost:9200";
        public const string BlobTenantId = "XXXXXXX";
        public const string BlobAccountName = "XXXXXXX";
    }
}