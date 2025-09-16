using System;
using System.Runtime.Caching;

namespace StackOverFlowExtractionTool.Services;

public class CacheService : ICacheService
{
    private readonly MemoryCache _cache = MemoryCache.Default;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);
    private int _hits;
    private int _misses;

    public void Add(string key, object data, TimeSpan? expiration = null)
    {
        var cacheItem = new CacheItem(key, data);
        var policy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.Add(expiration ?? _defaultExpiration),
            RemovedCallback = args => { if (args.RemovedReason == CacheEntryRemovedReason.Evicted) _misses++; }
        };
        
        _cache.Add(cacheItem, policy);
    }

    public T Get<T>(string key)
    {
        if (_cache.Contains(key))
        {
            _hits++;
            return (T)_cache.Get(key)!;
        }
        _misses++;
        return default!;
    }

    public bool Contains(string key)
    {
        return _cache.Contains(key);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public void Clear()
    {
        foreach (var item in _cache)
        {
            _cache.Remove(item.Key);
        }
    }
    
    public (int Hits, int Misses, int Total) GetStats()
    {
        return (_hits, _misses, _hits + _misses);
    }
}