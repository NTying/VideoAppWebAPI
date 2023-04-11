using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Core;
using StackExchange.Redis;
using System.Text;

namespace TyingVideoWebAPI.Utils
{
    public class RedisHelper<T>
    {
        const double WEEK_SECONDS = 7 * 24 *60 * 60;

        private readonly ILogger<RedisHelper<T>> _logger;
        private readonly IConnectionMultiplexer _connection;
        private IDatabase _db;
        private readonly IServer _server;

        public RedisHelper(IConnectionMultiplexer connection,IServer server, ILogger<RedisHelper<T>> logger)
        {
            _connection = connection;
            _server = server;
            _db = connection.GetDatabase(-1);
            _logger = logger;
        }

        /// <summary>
        /// 设置数据库
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public void SetDatabase(int? db = null)
        {
            try
            {
                _db = _connection.GetDatabase(db ?? -1);
                _logger.LogDebug("设置数据库{@database}成功", new { _db.Database }) ;
            }
            catch(Exception ex)
            {
                _logger.LogWarning("设置数据库{@database}失败", ex);
            }
        }

        /// <summary>
        /// Get all keys match the pattern, default is all
        /// </summary>
        /// <param name="database"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> GetKeys(int database=-1, string pattern="*")
        {        
            return _server.Keys(database:database, pattern:pattern).Select(x => x.ToString());
        }

        /// <summary>
        /// Set the cache expire time
        /// </summary>
        /// <param name="key"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task<bool> SetExpire(string key, double? time)
        {
            return _db.KeyExpireAsync(key, TimeSpan.FromSeconds(Convert.ToDouble(time)));
        }

        /// <summary>
        /// Check if the key exist
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> HasKey(string key)
        {
            return _db.KeyExistsAsync(key);
        }

        /// <summary>
        /// Delete specific key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> DeleteKey(string key)
        {
            return _db.KeyDeleteAsync(key);
        }

        /// <summary>
        /// 设置键、值、过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        public virtual void Set(string key, object data, double? cacheTime = WEEK_SECONDS)
        {
            if (data == null)
            {
                return;
            }
            var entryBytes = Serialize(data);
            if (cacheTime != null)
            {
                var expiresIn = TimeSpan.FromSeconds(Convert.ToDouble(cacheTime));
                _db.StringSet(key, entryBytes, expiresIn);
            }
            else
            {
                _db.StringSet(key, entryBytes);
            }

        }

        /// <summary>
        /// 根据键获取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual T? Get(string key)
        {
            var rValue = _db.StringGet(key);
            if (!rValue.HasValue)
            {
                return default;
            }

            var result = Deserialize(rValue);

            return result;
        }

        /// <summary>
        /// 序列化为 byte[]
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Serialize(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        protected virtual T? Deserialize(byte[] serializedObject)
        {
            if (serializedObject == null)
            {
                return default;
            }
            var json = Encoding.UTF8.GetString(serializedObject);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
