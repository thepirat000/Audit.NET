namespace Audit.WebApi
{
    public class BodyContent
    {
        public string Type { get; set; }
        public long? Length { get; set; }
        public object Value { get; set; }
    }
}
