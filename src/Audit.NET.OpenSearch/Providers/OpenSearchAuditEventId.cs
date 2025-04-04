using OpenSearch.Client;

namespace Audit.OpenSearch.Providers
{
    /// <summary>
    /// Identifies an indexed Audit Event in OpenSearch
    /// </summary>
    public class OpenSearchAuditEventId
    {
        public IndexName Index { get; set; }
        public Id Id { get; set; }
    }
}
