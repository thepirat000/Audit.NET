using System;
using System.IO;
using System.Threading.Tasks;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Audit.Core
{
    /// <summary>
    /// Adapter interface for JSON operations
    /// </summary>
    public interface IJsonAdapter
    {
        /// <summary>
        /// Serializes the specified object to JSON.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The serialized JSON string</returns>
        string Serialize(object value);
        /// <summary>
        /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>The deserialized object</returns>
        T Deserialize<T>(string json);
        /// <summary>
        /// Deserializes the specified JSON string to an object of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="type">The type of the object to deserialize.</param>
        /// <returns>The deserialized object</returns>
        object Deserialize(string json, Type type);
        /// <summary>
        /// Asynchronously serializes the specified object to JSON.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="value">The value.</param>
        /// <returns>The serialized JSON string</returns>
        Task SerializeAsync(Stream stream, object value);
        /// <summary>
        /// Asynchronously deserializes the specified JSON stream to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="stream">The stream to read the JSON input.</param>
        /// <returns>The deserialized object</returns>
        Task<T> DeserializeAsync<T>(Stream stream);

        T ToObject<T>(object value);
    }
}