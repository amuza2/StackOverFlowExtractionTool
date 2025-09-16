using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStackOverflowService _stackOverflowService;
    private readonly ICacheService _cacheService;

    [ObservableProperty]
    private string _tag = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Enter a tag and click Search";

    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _questions = new();

    [ObservableProperty]
    private StackOverflowQuestion? _selectedQuestion;
    
    public List<PageSizeOption> Pagesizes {get;} = new()
    {
        new PageSizeOption(10),
        new PageSizeOption(20),
        new PageSizeOption(30),
        new PageSizeOption(50),
    };
    
    [ObservableProperty]
    private PageSizeOption _selectedPageSize;
    
    [ObservableProperty]
    private bool _canGoToPreviousPage;

    [ObservableProperty] private bool _canGoToNextPage;
    
    [ObservableProperty]
    private bool _isUsingCache;
    
    [ObservableProperty]
    private string _cacheStatus = "Cache: Ready";
    
    [ObservableProperty]
    private int _cacheHits;
    
    [ObservableProperty]
    private int _cacheMisses;
    
    [ObservableProperty]
    private int _totalCacheRequests;

    [ObservableProperty]
    private string _cacheHitRate = "0%";
    

    public MainWindowViewModel(IStackOverflowService stackOverflowService, ICacheService cacheService)
    {
        _stackOverflowService = stackOverflowService ?? throw new ArgumentNullException(nameof(stackOverflowService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        SelectedPageSize = Pagesizes.First(x => x.Value == 10);
    }

    public MainWindowViewModel() : this(null!, null!) { }

    partial void OnSelectedPageSizeChanged(PageSizeOption obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        
        var option = Pagesizes.FirstOrDefault(x => x.Value == obj?.Value);
        if (option != null && option != SelectedPageSize)
            SelectedPageSize = option;

        if (!string.IsNullOrWhiteSpace(Tag) && !IsLoading)
        {
            _ = SearchQuestions();
        }
        
        StatusMessage = $"Page size changed to {obj.Value}. {StatusMessage}";
    }
    
    partial void OnTagChanged(string value)
    {
        UpdateCacheStatistics();
    }
    
    [RelayCommand]
    private async Task SearchQuestions()
    {
        if (string.IsNullOrWhiteSpace(Tag))
        {
            StatusMessage = "Please enter a tag";
            return;
        }

        IsLoading = true;
        IsUsingCache = false;
        StatusMessage = $"Loading questions for tag: {Tag}...";

        try
        {
            var questions = await _stackOverflowService.GetRecentQuestionsByTagAsync(Tag.Trim(), 1, SelectedPageSize.Value + 1);
            
            UpdateCacheStatistics();
            
            Questions.Clear();
            
            var questionsToShow = questions.Take(SelectedPageSize.Value).ToList();
            
            IsUsingCache = questionsToShow.Count > 0 && _cacheService.Contains(GenerateCacheKey());
            
            foreach (var question in questionsToShow)
            {
                Questions.Add(question);
            }
            
            UpdateCacheStatus();
            
            StatusMessage = questions.Count > 0 
                ? $"Found {questions.Count} questions for tag: {Tag}{(IsUsingCache ? " (Cached)" : "")}"
                : $"No questions found for tag: {Tag}";
        }
        catch (ArgumentException ex)
        {
            StatusMessage = $"Invalid input: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearCache()
    {
        _cacheService.Clear();
        UpdateCacheStatus();
        StatusMessage = "Cache cleared";
    }
    
    [RelayCommand]
    private void ClearCacheForCurrentTag()
    {
        var cacheKey = GenerateCacheKey();
        if (_cacheService.Contains(cacheKey))
        {
            _cacheService.Remove(cacheKey);
            UpdateCacheStatus();
            StatusMessage = $"Cache cleared for tag: {Tag}";
        }
        else
        {
            StatusMessage = "No cache found for current tag";
        }
    }
    
    private string GenerateCacheKey()
    {
        var timeSegment = DateTime.Now.ToString("yyyyMMddHH");
        return $"questions_{Tag.ToLowerInvariant()}_size{SelectedPageSize.Value}_{timeSegment}";
    }
    
    private void UpdateCacheStatus()
    {
        var cacheKey = GenerateCacheKey();
        CacheStatus = _cacheService.Contains(cacheKey) 
            ? "Cache: Hit" 
            : "Cache: Miss";
    }
    
    private void UpdateCacheStatistics()
    {
        var stats = _cacheService.GetStats();
        CacheHits = stats.Hits;
        CacheMisses = stats.Misses;
        TotalCacheRequests = stats.Total;
        
        CacheHitRate = TotalCacheRequests > 0 
            ? $"{(CacheHits * 100.0 / TotalCacheRequests):F1}%" 
            : "0%";
    }
    

    [RelayCommand]
    private void OpenQuestionInBrowser(StackOverflowQuestion question)
    {
            try
            {
                var url = question.Link;
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
                StatusMessage = $"Opened in browser: {question.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening browser: {ex.Message}";
            }
    }

    [RelayCommand]
    private void ClearResults()
    {
        Questions.Clear();
        Tag = string.Empty;
        SelectedQuestion = null;
        StatusMessage = "Enter a tag and click Search";
    }
    
    [RelayCommand] 
    private async Task RefreshQuestions()
    {
        if (!string.IsNullOrWhiteSpace(Tag))
        {
            await SearchQuestions();
        }
    }
}