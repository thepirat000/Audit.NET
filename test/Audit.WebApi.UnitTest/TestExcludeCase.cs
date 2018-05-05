using System.Net;

namespace Audit.WebApi.UnitTest
{
    public class TestExcludeCase
    {
        public HttpStatusCode[] ExcludeList { get; set; }
        public HttpStatusCode[] IncludeList { get; set; }
        public bool IncludeBoolean { get; set; }

        public bool ExpectInclude_200 { get; set; }
        public bool ExpectInclude_400 { get; set; }
    }

}
