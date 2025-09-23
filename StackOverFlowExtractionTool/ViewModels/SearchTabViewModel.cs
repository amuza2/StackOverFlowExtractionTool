using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Extensions;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class SearchTabViewModel : ViewModelBase
{
    private readonly IStackOverflowService _stackOverflowService;
    private readonly ICacheService _cacheService;
    private readonly INotificationService _notificationService;
    
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

    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _allQuestions = new();

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isUsingCache;

    [ObservableProperty]
    private bool _canGoToPreviousPage;

    [ObservableProperty]
    private bool _canGoToNextPage;

    public FilterOptions FilterOptions { get; } = new();
    public List<PageSizeOption> PageSizes { get; } = new()
    {
        new PageSizeOption(10), new PageSizeOption(20), 
        new PageSizeOption(30), new PageSizeOption(50)
    };

    [ObservableProperty]
    private PageSizeOption _selectedPageSize;
    
    [ObservableProperty] private bool _enablePopupNotification = true;
    public string NotificationPopupState => EnablePopupNotification ? "🔔 Pop-up Enabled" : "🔕 Pop-up Disabled";
    public string BellIcon => EnablePopupNotification ? "🔔" : "🔕";

    public SearchTabViewModel(IStackOverflowService stackOverflowService, ICacheService cacheService, INotificationService notificationService)
    {
        _stackOverflowService = stackOverflowService;
        _cacheService = cacheService;
        _notificationService = notificationService;
        SelectedPageSize = PageSizes.First(x => x.Value == 30);
        FilterOptions.FilterChanged += OnFilterChanged;
    }

    public SearchTabViewModel() : this(null!, null!, null!) { }

    partial void OnSelectedPageSizeChanged(PageSizeOption obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        
        var option = PageSizes.FirstOrDefault(x => x.Value == obj?.Value);
        if (option != null && option != SelectedPageSize)
            SelectedPageSize = option;
    
        if (!string.IsNullOrWhiteSpace(Tag) && !IsLoading)
        {
            _ = SearchQuestions();
        }
        
        StatusMessage = $"Page size changed to {obj.Value}. {StatusMessage}";
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
            var questions = await _stackOverflowService.GetRecentQuestionsByTagAsync(Tag.Trim(), 1, SelectedPageSize.Value);
            var questionsToShow = questions.Take(SelectedPageSize.Value).ToList();
            
            IsUsingCache = questionsToShow.Count > 0 && _cacheService.Contains(GenerateCacheKey());

            AllQuestions.Clear();
            foreach (var question in questionsToShow)
            {
                AllQuestions.Add(question);
            }
            
            ApplyFilter();
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

    private void ApplyFilter()
    {
        if (AllQuestions.Count == 0)
        {
            Questions.Clear();
            FilteredCount = 0;
            TotalCount = 0;
            return;
        }

        var filtered = FilterOptions.ApplyFilter(AllQuestions).ToList();

        Questions.Clear();
        foreach (var question in filtered)
        {
            Questions.Add(question);
        }

        FilteredCount = filtered.Count;
        TotalCount = AllQuestions.Count;
        
        UpdateStatusMessage();
    }
    
    private void UpdateStatusMessage()
    {
        if (TotalCount == 0)
        {
            StatusMessage = "No questions found";
            return;
        }

        if (FilterOptions.IsFilterActive && FilterOptions.SelectedFilter?.DisplayName != "All Questions")
        {
            StatusMessage = $"Showing {FilteredCount} of {TotalCount} questions ({FilterOptions.SelectedFilter?.DisplayName})";
        }
        else
        {
            StatusMessage = $"Showing {TotalCount} questions";
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        FilterOptions.ClearFilter();
        ApplyFilter();
        StatusMessage = "Filter cleared";
    }

    [RelayCommand]
    private void ClearCache()
    {
        _cacheService.Clear();
        StatusMessage = "Cache cleared";
    }

    [RelayCommand]
    private void ClearCacheForCurrentTag()
    {
        var cacheKey = GenerateCacheKey();
        if (_cacheService.Contains(cacheKey))
        {
            _cacheService.Remove(cacheKey);
            StatusMessage = $"Cache cleared for tag: {Tag}";
        }
        else
        {
            StatusMessage = "No cache found for current tag";
        }
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
            ClearCacheCommand.Execute(null);
            await SearchQuestions();
        }
    }

    [RelayCommand]
    private async Task CopyLink(string url)
    {
        try
        {
            if (await ClipboardHelper.SetTextAsync(url))
            {
                StatusMessage = "Link copied to clipboard";
            }
            else
            {
                StatusMessage = "Failed to copy link";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to copy link: {ex.Message}";
        }
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        if (AllQuestions.Count > 0)
            ApplyFilter();
    }

    private string GenerateCacheKey()
    {
        var timeSegment = DateTime.Now.ToString("yyyyMMddHH");
        return $"questions_{Tag.ToLowerInvariant()}_size{SelectedPageSize.Value}_{timeSegment}";
    }
    
    [RelayCommand]
    private void ToggleNotificationPopup()
    {
        EnablePopupNotification = _notificationService.TogglePopupNotifications();
        OnPropertyChanged(nameof(NotificationPopupState));
        OnPropertyChanged(nameof(BellIcon));
    }
    
}