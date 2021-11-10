﻿#if NET45
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Audit.Core;

namespace Audit.Mvc
{
    internal static class AuditHelper
    {
        internal static IDictionary<string, object> SerializeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return null;
            }
            return parameters.ToDictionary(
                k => k.Key, 
                v => v.Value == null ? null : Configuration.JsonAdapter.Deserialize(Configuration.JsonAdapter.Serialize(v.Value), v.Value.GetType()));
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