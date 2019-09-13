using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;
using System.Threading.Tasks;
using Murmur;
using System.Security.Cryptography;

namespace CacheInvalidationRoute
{
    public class RedisCacheService
    {
        private IDatabase Cache;
        private ConnectionMultiplexer ConnectionMultiplexer;


        internal RedisCacheService(string host, int port)
        {
            if (!String.IsNullOrEmpty(host))
            {
                InitiateConnection(host);
            }
        }

        public bool IsConnectionActive()
        {
            if (Cache != null && ConnectionMultiplexer != null)
                return ConnectionMultiplexer.IsConnected;

            return false;
        }

        void InitiateConnection(string host = null)
        {
            try
            {
                if (Cache == null || !ConnectionMultiplexer.IsConnected)
                {
                    if (!String.IsNullOrEmpty(host))
                    {
                        ConnectionMultiplexer = ConnectionMultiplexer.Connect(host);
                        Cache = ConnectionMultiplexer.GetDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Exists(string key)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(key))
                {
                    key = key.ToUpper();

                    return Cache.KeyExists(key);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public async Task<bool> Exists(string hashSetKey, string objectKey)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    objectKey = objectKey.ToUpper();

                    return await Cache.HashExistsAsync(hashSetKey, objectKey);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return false;
        }

        public async Task<string> Get(string key)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(key))
                {
                    key = key.ToUpper();
                    return await Cache.StringGetAsync(key);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public string Get(string hashSetKey, string objectKey)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    objectKey = objectKey.ToUpper();

                    return Cache.HashGet(hashSetKey, objectKey);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public dynamic GetRedisValue(string hashSetKey, string objectKey)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    objectKey = objectKey.ToUpper();

                    return Cache.HashGet(hashSetKey, objectKey);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public async Task Save(string key, string value, long ttl = -1)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(key) && !String.IsNullOrEmpty(value))
                {
                    key = key.ToUpper();

                    var task = await Cache.StringSetAsync(key, value);

                    if (ttl > -1)
                    {
                        DateTime expireTime = DateTime.Now;
                        await Cache.KeyExpireAsync(key, expireTime.AddMinutes(ttl));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task Save(string hashSetKey, string objectKey, string value, long ttl = -1)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey) && !String.IsNullOrEmpty(value))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    objectKey = objectKey.ToUpper();

                    var task = await Cache.HashSetAsync(hashSetKey, objectKey, value);

                    if (ttl > -1)
                    {
                        DateTime expireTime = DateTime.Now;
                        await Cache.KeyExpireAsync(objectKey, expireTime.AddMinutes(ttl));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task Save(string hashSetKey, string objectKey, RedisValue value, long ttl = -1)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey) &&
                    !String.IsNullOrEmpty(value))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    objectKey = objectKey.ToUpper();

                    var task = await Cache.HashSetAsync(hashSetKey, objectKey, value);

                    // Populate my result object with the necessary values
                    if (ttl > -1)
                    {
                        DateTime expireTime = DateTime.Now;
                        await Cache.KeyExpireAsync(objectKey, expireTime.AddMinutes(ttl));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void RemoveAllKeysInHash(string hashSetKey)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(hashSetKey))
                {
                    hashSetKey = hashSetKey.ToUpper();
                    var allKeys = Cache.HashGetAll(hashSetKey);

                    Parallel.ForEach(allKeys, key =>
                    {
                        Cache.HashDeleteAsync(hashSetKey.ToUpper(), key.Name.ToString());
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Remove(string key)
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                if (!String.IsNullOrEmpty(key))
                {
                    key = key.ToUpper();
                    Cache.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void RemovePattern(string patternstring)
        {
            try
            {
                if (!string.IsNullOrEmpty(patternstring))
                {
                    if (!ConnectionMultiplexer.IsConnected || Cache == null)
                        InitiateConnection();

                    var endpoints = ConnectionMultiplexer.GetEndPoints();
                    Parallel.ForEach(endpoints, endpoint =>
                    {
                        var server = ConnectionMultiplexer.GetServer(endpoint);

                        Parallel.ForEach(server.Keys(pattern: patternstring + "*"), key =>
                        {
                            Cache.KeyDelete(key);
                        });
                    });
                }
            }
            catch { }
        }

        public void Clear()
        {
            try
            {
                if (!ConnectionMultiplexer.IsConnected || Cache == null)
                    InitiateConnection();

                var endpoints = ConnectionMultiplexer.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = ConnectionMultiplexer.GetServer(endpoint);

                    Parallel.ForEach(server.Keys(), key =>
                    {
                        Console.WriteLine(key);
                        Cache.KeyDelete(key);
                    });
                }
            }
            catch (Exception ex)
            {
                if (Cache == null)
                    InitiateConnection();

                throw ex;
            }
        }

        public void Dispose()
        {
            if (ConnectionMultiplexer.IsConnected)
                ConnectionMultiplexer.Dispose();
        }
    }

    class CacheHelper
    {
        private static RedisCacheService WebCacheService;
        private static TimeSpan _cacheTimeout = TimeSpan.FromMilliseconds(1000);

        private static void InitCacheConnection()
        {
            try
            {
                if ((WebCacheService == null || !WebCacheService.IsConnectionActive()))
                    WebCacheService = new RedisCacheService(Config.WebCacheUrl, 6379);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: CACHE InitCacheConnection - {ex.ToString()}");
            }
        }

        public static void DeleteCacheEntityWithUrl(string url)
        {
            try
            {
                if (WebCacheService == null)
                    InitCacheConnection();

                if (WebCacheService != null)
                {
                    var dnsHost = string.Empty;
                    try
                    {
                        dnsHost = new Uri(url).DnsSafeHost?.ToUpper();
                    }
                    catch { }

                    var cacheHashKey = HashUrl.GetHashAsString(dnsHost);

                    var task = Task.Run(() => WebCacheService.RemoveAllKeysInHash(cacheHashKey));
                    if (!task.Wait(_cacheTimeout))
                        Console.WriteLine($"ERROR: CACHE DeleteCacheEntityWithUrl - Redis TimeOut");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: CACHE DeleteCacheEntity - {ex.ToString()}");
            }
        }
    }

    public class HashUrl
    {
        public static string GetHashAsString(string key)
        {
            try
            {
                if (String.IsNullOrEmpty(key))
                    throw new Exception("Incorrect key");

                HashAlgorithm murmur128 = MurmurHash.Create128(0, true, AlgorithmPreference.X86);
                var hash = murmur128.ComputeHash(Encoding.ASCII.GetBytes(key));

                if (hash.Length < 16)
                    throw new Exception("Hash Length must be more than 16 byte");

                var builder = new StringBuilder(16);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));

                return builder.ToString().ToUpper();
            }
            catch { throw; }
        }
    }
}
