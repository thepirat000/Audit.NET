#if NET45
using System.Web.Http.ModelBinding;
#else
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endif
using Audit.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Audit.WebApi
{
    internal static class AuditApiHelper
    {
        internal const string AuditApiActionKey = "__private_AuditApiAction__";
        internal const string AuditApiScopeKey = "__private_AuditApiScope__";


        internal static IDictionary<string, object> SerializeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return null;
            }
            return parameters.ToDictionary(
                k => k.Key, 
                v => v.Value == null ? null : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(v.Value, Configuration.JsonSettings), v.Value.GetType(), Configuration.JsonSettings));
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

#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NETCOREAPP3_1
        internal static IDictionary<string, string> ToDictionary(IEnumerable<KeyValuePair<string, StringValues>> col)
        {
            if (col == null)
            {
                return null;
            }
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var k in col)
            {
                dict.Add(k.Key, string.Join(", ", k.Value));
            }
            return dict;
        }


        internal static IDictionary<string, string> GetFormVariables(HttpContext context)
        {
            if (!context.Request.HasFormContentType)
            {
                return null;
            }
            IFormCollection formCollection;
            try
            {
                formCollection = context.Request.Form;
            }
            catch (InvalidDataException)
            {
                // InvalidDataException could be thrown if the form count exceeds the limit, etc
                return null;
            }
            return ToDictionary(formCollection);
        }

        internal static async Task<string> GetRequestBody(HttpContext httpContext)
        {
            var body = httpContext.Request.Body;
            if (body != null && body.CanRead)
            {
                using (var stream = new MemoryStream())
                {
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    await body.CopyToAsync(stream);
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            return null;
        }

        internal static async Task<string> GetResponseBody(HttpContext httpContext)
        {
            var body = httpContext.Response.Body;
            if (body != null && body.CanRead)
            {
                using (var stream = new MemoryStream())
                {
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    await body.CopyToAsync(stream);
                    if (body.CanSeek)
                    {
                        body.Seek(0, SeekOrigin.Begin);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            return null;
        }


        internal static string GetStatusCodeString(int statusCode)
        {
            var name = ((HttpStatusCode)statusCode).ToString();
            string[] words = Regex.Matches(name, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();
            return words.Length == 0 ? name : string.Join(" ", words);
        }

#endif

    }
}
