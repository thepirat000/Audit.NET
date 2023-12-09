namespace Audit.MongoClient
{
    public class MongoConnection
    {
        /// <summary>
        /// The Connection cluster identifier.
        /// </summary>
        public int ClusterId { get; set; }

        /// <summary>
        /// The connection endpoint
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// The local connection identifier
        /// </summary>
        public long LocalConnectionId { get; set; }

        /// <summary>
        /// The server connection identifier
        /// </summary>
        public long? ServerConnectionId { get; set; }
    }
}