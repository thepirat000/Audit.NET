#if IS_COSMOS
using System.IO;
using System.Text;
using Microsoft.Azure.Cosmos;

namespace Audit.AzureCosmos.Providers
{
#pragma warning disable CS3009 // Base type is not CLS-compliant
    public class AuditCosmosSerializer : CosmosSerializer
#pragma warning restore CS3009 // Base type is not CLS-compliant
    {
        public override T FromStream<T>(Stream stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default;
            }
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }
            using (stream)
            {
                using (var sr = new StreamReader(stream))
                {
                    return Core.Configuration.JsonAdapter.Deserialize<T>(sr.ReadToEnd());
                }
            }
        }
        public override Stream ToStream<T>(T input)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(Core.Configuration.JsonAdapter.Serialize(input)));
            ms.Position = 0;
            return ms;
        }
    }
}
#endif