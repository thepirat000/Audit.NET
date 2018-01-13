#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Audit.Mvc
{
    internal static class AuditHelper
    {
        private static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        internal static IDictionary<string, object> SerializeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return null;
            }
            return parameters.ToDictionary(
                k => k.Key, 
                v => v.Value == null ? null : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(v.Value, _serializerSettings), v.Value.GetType(), _serializerSettings));
        }

        internal static Dictionary<string, string> GetModelStateErrors(ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                return null;
            }
            var dict = new Dictionary<string, string>();
            foreach (var state in modelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    dict.Add(state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }
            return dict.Count > 0 ? dict : null;
        }
    }
}
#endif