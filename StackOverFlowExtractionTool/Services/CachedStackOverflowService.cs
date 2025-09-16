using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackOverFlowExtractionTool.Models;

namespace StackOverFlowExtractionTool.Services;

public class CachedStackOverflowService : IStackOverflowService
{
    private readonly IStackOverflowService _decoratedService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedStackOverflowService> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);

    public CachedStackOverflowService(IStackOverflowService decoratedService, ICacheService cacheService, ILogger<CachedStackOverflowService> logger)
    {
        _decoratedService = decoratedService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<StackOverflowQuestion>> GetRecentQuestionsByTagAsync(string tag, int page = 1, int pageSize = 20)
    {
        var cacheKey = GenerateCacheKey(tag, page, pageSize);
        
        _logger.LogDebug("Checking cache for key: {CacheKey}", cacheKey);
        
        // Try to get from cache first
        if (_cacheService.Contains(cacheKey))
        {
            _logger.LogInformation("Cache HIT for tag: {Tag}, page: {Page}", tag, page);
            return _cacheService.Get<List<StackOverflowQuestion>>(cacheKey);
        }

        _logger.LogInformation("Cache MISS for tag: {Tag}, page: {Page}. Fetching from API...", tag, page);
        // If not in cache, fetch from API
        var questions = await _decoratedService.GetRecentQuestionsByTagAsync(tag, page, pageSize);
        
        // Cache the results
        if (questions.Count > 0)
        {
            _logger.LogInformation("Caching {QuestionCount} questions for tag: {Tag}", questions.Count, tag);
            _cacheService.Add(cacheKey, questions, _cacheDuration);
        }
        else
        {
            _logger.LogWarning("No questions found to cache for tag: {Tag}", tag);
        }

        return questions;
    }

    private string GenerateCacheKey(string tag, int page, int pageSize)
    {
        var timeSegment = DateTime.Now.ToString("yyyyMMddHH");
        return $"questions_{tag.ToLowerInvariant()}_{timeSegment}";
    }
}