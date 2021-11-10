namespace Audit.EntityFramework
{
    /// <summary>
    /// Describes a table by its schema and name
    /// </summary>
    public class EntityName
    {
        public string Schema { get; set; }
        public string Table { get; set; }
    }
}
