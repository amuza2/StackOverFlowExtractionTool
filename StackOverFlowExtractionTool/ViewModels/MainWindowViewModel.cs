using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IStackOverflowService _stackOverflowService;
    private readonly ICacheService _cacheService;
    private readonly INotificationService _notificationService;
    private bool _disposed;

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

    private int questionsToSearchFor = 30;
    
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
    private FilterOptions _filterOptions = new();

    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _allQuestions = new();

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private int _totalCount;
    
    [ObservableProperty]
    private bool _isMonitoring;
    
    [ObservableProperty]
    private string _monitoringStatus = "Monitoring: Off";
    
    [ObservableProperty]
    private NotificationViewModel _notificationViewModel;
    
    [ObservableProperty]
    private int _selectedTabIndex = 0;
    
    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _recentQuestions = new();
    
    public List<TagSubscription> Subscriptions => _notificationService.GetSubscriptions();

    public MainWindowViewModel(IStackOverflowService stackOverflowService, ICacheService cacheService, INotificationService notificationService, NotificationViewModel notificationViewModel)
    {
        _stackOverflowService = stackOverflowService ?? throw new ArgumentNullException(nameof(stackOverflowService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _notificationService = notificationService;
        NotificationViewModel = notificationViewModel;
        SelectedPageSize = Pagesizes.First(x => x.Value == questionsToSearchFor);
        FilterOptions.FilterChanged += OnFilterChanged;
        
        // Subscribe to notification events
        _notificationService.NotificationReceived += OnNotificationReceived;
        _notificationService.NewQuestionDetected += OnNewQuestionDetected;
    }

    private void OnNewQuestionDetected(object? sender, StackOverflowQuestion question)
    {
        AddToRecentQuestions(question);
    }

    private void OnNotificationReceived(object? sender, Notification notification)
    {
        NotificationViewModel.AddNotification(notification);
    }
    
    private void AddToRecentQuestions(StackOverflowQuestion question)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Check if we already have this question (by ID)
            var existing = RecentQuestions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
            if (existing != null)
            {
                // Update existing question with new data
                RecentQuestions.Remove(existing);
            }
        
            RecentQuestions.Insert(0, question);
        
            // Keep only the most recent 50 questions
            if (RecentQuestions.Count > 50)
            {
                RecentQuestions.RemoveAt(RecentQuestions.Count - 1);
            }

            // Update status if we're on the recent questions tab
            if (SelectedTabIndex == 1)
            {
                StatusMessage = $"New question added: {question.Title}";
            }
        });
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        if (AllQuestions.Count > 0)
            ApplyFilter();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            FilterOptions.FilterChanged -= OnFilterChanged;
            _notificationService.StopMonitoring();
            _notificationService.NotificationReceived -= OnNotificationReceived;
            _notificationService.NewQuestionDetected -= OnNewQuestionDetected;
            _disposed = true;
        }
    }

    public MainWindowViewModel() : this(null!, null!, null!, null!) { }

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
    
    private string GenerateCacheKey()
    {
        var timeSegment = DateTime.Now.ToString("yyyyMMddHH");
        return $"questions_{Tag.ToLowerInvariant()}_size{SelectedPageSize.Value}_{timeSegment}";
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
    
    [RelayCommand]
    private void SubscribeToCurrentTag()
    {
        if (string.IsNullOrWhiteSpace(Tag))
        {
            StatusMessage = "Please enter a tag first";
            return;
        }

        _notificationService.SubscribeToTag(Tag.Trim());
        var subs = _notificationService.GetSubscriptions();
        Console.WriteLine($"Total subscriptions after adding {Tag}: {subs.Count}");
        foreach (var sub in subs)
        {
            Console.WriteLine($"- {sub.Tag} (Active: {sub.IsActive})");
        }
        
        StatusMessage = $"Subscribed to tag: {Tag}";
    }

    [RelayCommand]
    private async Task ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _notificationService.StopMonitoring();
            IsMonitoring = false;
            MonitoringStatus = "Monitoring: Off";
            StatusMessage = "Monitoring stopped";
        }
        else
        {
            await _notificationService.StartMonitoringAsync();
            IsMonitoring = true;
            MonitoringStatus = "Monitoring: On";
            StatusMessage = "Monitoring started";
        }
    }
    
    [RelayCommand]
    private void TestNotification()
    {
        _notificationService.TestNotification();
        StatusMessage = "Test notification sent!";
    }
    
    [RelayCommand]
    private void ClearRecentQuestions()
    {
        RecentQuestions.Clear();
        StatusMessage = "Recent questions cleared";
    }
    
    [RelayCommand]
    private void ShowRecentQuestions()
    {
        SelectedTabIndex = 1;
    }
}