using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.WebApi.Template.Providers
{
    public interface IValuesProvider
    {
        IEnumerable<string> GetValues();
        Task<string> GetAsync(int id);
        Task<int> InsertAsync(string value);
        Task ReplaceAsync(int id, string value);
        Task<bool> DeleteAsync(int id);
        Task<int> DeleteMultipleAsync(int[] ids);
    }
}
