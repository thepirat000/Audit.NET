namespace Audit.WebApi.Template
{
    /// <summary>
    /// Custom Audit Scope Factory that includes information to the audit events from the HttpContext
    /// </summary>
    public class MyAuditScopeFactory : AuditScopeFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public MyAuditScopeFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        /// <inheritdoc />
        public override void OnConfiguring(AuditScopeOptions options)
        {
        }

        /// <inheritdoc />
        public override void OnScopeCreated(AuditScope auditScope)
        {
            auditScope.SetCustomField("TraceId", _httpContextAccessor.HttpContext?.TraceIdentifier);
            auditScope.SetCustomField("UserName", _httpContextAccessor.HttpContext?.User.Identity?.Name);
        }
    }
}
