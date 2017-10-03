namespace Audit.PostgreSql.Configuration
{
    public enum DataType
    {
        /// <summary>JSON data type</summary>
        JSON = 0,
        /// <summary>JSONB data type</summary>
        JSONB = 1,
        /// <summary>Any valid character string type (text, char, varchar)</summary>
        String = 2
    }
}
