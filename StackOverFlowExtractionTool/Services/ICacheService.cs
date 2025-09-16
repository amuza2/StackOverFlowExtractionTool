using System;

namespace StackOverFlowExtractionTool.Services;

public interface ICacheService
{
    void Add(string key, object data, TimeSpan? expiration = null);
    T Get<T>(string key);
    bool Contains(string key);
    void Remove(string key);
    void Clear();
    (int Hits, int Misses, int Total) GetStats();
}