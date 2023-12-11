#if ASP_CORE

namespace Audit.Mvc.UnitTest
{
    public class MockMethodInfo
    {
        public void Method1(string test1, AuditAttribute x, string extra)
        {

        }
        [AuditIgnore]
        public void Method1_Ignored(string test1, AuditAttribute x, string extra)
        {

        }
        
        public void Method1_IgnoredParam([AuditIgnore]string test1, AuditAttribute x, [AuditIgnore]string extra)
        {

        }
    }
}
#endif