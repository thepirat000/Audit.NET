namespace Audit.Http
{
    public class HttpAction
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public Request Request { get; set; }
        public Response Response { get; set; }
        public string Exception { get; set; }
    }
}
