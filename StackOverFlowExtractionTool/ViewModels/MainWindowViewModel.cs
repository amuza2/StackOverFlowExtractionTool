using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly INotificationService _notificationService;
    private bool _disposed;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private NotificationViewModel _notificationViewModel;
    
    [ObservableProperty]
    private SearchTabViewModel _searchTab;
    
    [ObservableProperty]
    private RecentQuestionsViewModel _recentQuestionsTab;
    
    [ObservableProperty]
    private SettingsViewModel _settingsTab;
    
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public MainWindowViewModel(
        IStackOverflowService stackOverflowService, 
        ICacheService cacheService, 
        INotificationService notificationService, 
        NotificationViewModel notificationViewModel,
        IAppSettingsService appSettingsService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        NotificationViewModel = notificationViewModel ?? throw new ArgumentNullException(nameof(notificationViewModel));
        
        // Initialize tab view models
        SearchTab = new SearchTabViewModel(stackOverflowService, cacheService, notificationService);
        RecentQuestionsTab = new RecentQuestionsViewModel(notificationService, notificationViewModel);
        SettingsTab = new SettingsViewModel(notificationService, appSettingsService, cacheService);
        
        // Subscribe to status message changes from SearchTab
        SearchTab.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SearchTab.StatusMessage))
            {
                StatusMessage = SearchTab.StatusMessage;
            }
        };
        
        // Subscribe to notification events
        _notificationService.NotificationReceived += OnNotificationReceived;
        _notificationService.NewQuestionDetected += OnNewQuestionDetected;
    }
    
    public MainWindowViewModel() : this(null!, null!, null!, null!, null!) { }

    private void OnNewQuestionDetected(object? sender, StackOverflowQuestion question)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RecentQuestionsTab.AddQuestion(question);
            
            // Update status if we're on the recent questions tab
            if (SelectedTabIndex == 1)
            {
                StatusMessage = $"New question added: {question.Title}";
            }
        });
    }

    private void OnNotificationReceived(object? sender, Notification notification)
    {
        NotificationViewModel.AddNotification(notification);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _notificationService.StopMonitoring();
            _notificationService.NotificationReceived -= OnNotificationReceived;
            _notificationService.NewQuestionDetected -= OnNewQuestionDetected;
            _disposed = true;
        }
    }
}