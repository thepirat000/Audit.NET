using System;
using OpenSearch.Client;

namespace Audit.OpenSearch.Extensions;

public static class OpenSearchExtensions
{
    public static bool TryGetOriginalException<T>(this T resp, out Exception? exception)
        where T : ResponseBase
    {
        if (resp.ApiCall?.OriginalException is not null)
        {
            exception = resp.ApiCall.OriginalException;
            return true;
        }

        exception = null;
        return false;
    }
}