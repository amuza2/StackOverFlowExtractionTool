using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackOverFlowExtractionTool.Models;

namespace StackOverFlowExtractionTool.Services;

public class StackOverflowService : IStackOverflowService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StackOverflowService> _logger;
    private bool _disposed = false; 

    public StackOverflowService(HttpClient httpClient,ILogger<StackOverflowService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://api.stackexchange.com/2.3/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "StackOverflowExtractor/1.0");
        }
    }

    public async Task<List<StackOverflowQuestion>> GetRecentQuestionsByTagAsync(string tag, int page = 1, int pageSize = 20)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StackOverflowService));
        
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be null or empty", nameof(tag));
        
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100");
        
        try
        {
            var url = $"questions?page={page}&pagesize={pageSize}&order=desc&sort=creation&tagged={Uri.EscapeDataString(tag)}&site=stackoverflow&filter=withbody";
            
            _logger?.LogInformation("Fetching questions for tag: {Tag}, Page: {Page}, PageSize: {PageSize}", tag, page, pageSize);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger?.LogWarning("Empty response received from Stack Overflow API");
                return new List<StackOverflowQuestion>();
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<StackOverflowResponse>(json, options);

            if (result?.Items == null)
            {
                _logger?.LogWarning("No items found in API response");
                return new List<StackOverflowQuestion>();
            }
            
            _logger?.LogInformation("Successfully fetched {Count} questions", result.Items.Count);
            return result.Items;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while fetching questions for tag: {Tag}", tag);
            throw new Exception($"Network error while fetching questions: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogError(ex, "Timeout while fetching questions for tag: {Tag}", tag);
            throw new Exception("Request timed out while fetching questions", ex);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error for tag: {Tag}", tag);
            throw new Exception("Invalid response format from Stack Overflow API", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while fetching questions for tag: {Tag}", tag);
            throw new Exception($"Failed to get recent questions: {ex.Message}", ex);
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}