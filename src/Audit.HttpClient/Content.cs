using System.Collections.Generic;

namespace Audit.Http
{
    public class Content
    {
        public object Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
