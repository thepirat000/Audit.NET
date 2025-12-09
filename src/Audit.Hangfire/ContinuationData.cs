using System;

namespace Audit.Hangfire;

public class ContinuationData
{
    public string ParentId { get; set; }
    public string Option { get; set; }
    public TimeSpan Expiration { get; set; }
}