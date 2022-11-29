using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if EnableEntityFramework
using Audit.WebApi.Template.Services.Database;
#else
using System;
using System.Collections.Concurrent;
#endif

namespace Audit.WebApi.Template.Services
{
#if EnableEntityFramework
    /// <summary>
    /// Sample service with CRUD operations on a Database 
    /// </summary>
    public class ValuesService : IValuesService
    {
        private readonly MyContext _dbContext;

        public ValuesService(MyContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public IEnumerable<string?> GetValues()
        {
            return _dbContext.Values.Select(x => x.Value);
        }

        public async Task<string?> GetAsync(int id)
        {
            var entity = await _dbContext.Values.FindAsync(id);
            return entity?.Value;
        }

        public async Task<int> InsertAsync(string value)
        {
            var entity = new ValueEntity() {Value = value};
            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity.Id;
        }

        public async Task ReplaceAsync(int id, string value)
        {
			var entity = await _dbContext.Values.FindAsync(id);
            if (entity != null)
            {
                entity.Value = value;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbContext.Values.FindAsync(id);
            if (entity != null)
            {
                _dbContext.Remove(entity);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<int> DeleteMultipleAsync(int[] ids)
        {
            int c = 0;
            foreach (int id in ids)
            {
                c += await DeleteAsync(id) ? 1 : 0;
            }
            return c;
        }
    }
#else
    /// <summary>
    /// Sample provider with CRUD operations on a dictionary
    /// </summary>
    public class ValuesService : IValuesService
    {
        private static Random _random = new Random();
        private static IDictionary<int, string> _data = new ConcurrentDictionary<int, string>();

        public async Task<string> GetAsync(int id)
        {
            _data.TryGetValue(id, out string value);
            return await Task.FromResult(value);
        }

        public IEnumerable<string> GetValues()
        {
            return _data.Values;
        }

        public async Task<int> InsertAsync(string value)
        {
            int key = _random.Next();
            _data[key] = value;
            return await Task.FromResult(key);
        }

        public async Task ReplaceAsync(int id, string value)
        {
            _data[id] = value;
            await Task.CompletedTask;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await Task.FromResult(_data.Remove(id));
        }

        public async Task<int> DeleteMultipleAsync(int[] ids)
        {
            int c = 0;
            foreach (int id in ids)
            {
                c += await DeleteAsync(id) ? 1 : 0;
            }
            return c;
        }
    }
#endif
}
