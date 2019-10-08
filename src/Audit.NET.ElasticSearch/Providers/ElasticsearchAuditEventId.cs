using Nest;

namespace Audit.Elasticsearch.Providers
{
    /// <summary>
    /// Identifies an indexed Audit Event in Elasticsearch
    /// </summary>
    public class ElasticsearchAuditEventId
    {
        public IndexName Index { get; set; }
        public Id Id { get; set; }
    }
}
