using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;
    
    [ObservableProperty]
    private bool _enablePopupNotification = true;
    
    [ObservableProperty]
    private bool _isMonitoring;
    
    [ObservableProperty]
    private string _monitoringStatus = "Monitoring: Off";
    
    [ObservableProperty]
    private ObservableCollection<TagSubscription> _subscriptions = new();
    
    [ObservableProperty]
    private int _cacheHits;
    
    [ObservableProperty]
    private int _cacheMisses;
    
    [ObservableProperty]
    private int _totalCacheRequests;
    
    [ObservableProperty]
    private string _cacheStatus = "Cache: Ready";

    public string NotificationPopupState => EnablePopupNotification ? "🔔 Pop-up Enabled" : "🔕 Pop-up Disabled";
    public string BellIcon => EnablePopupNotification ? "🔔" : "🔕";

    public SettingsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        
        // Subscribe to notification events
        _notificationService.NotificationReceived += OnNotificationReceived;
        _notificationService.NewQuestionDetected += OnNewQuestionDetected;
        
        UpdateSubscriptions();
    }

    public SettingsViewModel() : this(null!) { }

    [RelayCommand]
    private void ToggleNotificationPopup()
    {
        EnablePopupNotification = _notificationService.TogglePopupNotifications();
        OnPropertyChanged(nameof(NotificationPopupState));
        OnPropertyChanged(nameof(BellIcon));
    }

    [RelayCommand]
    private async Task ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _notificationService.StopMonitoring();
            IsMonitoring = false;
            MonitoringStatus = "Monitoring: Off";
        }
        else
        {
            await _notificationService.StartMonitoringAsync();
            IsMonitoring = true;
            MonitoringStatus = "Monitoring: On";
        }
    }

    [RelayCommand]
    private void TestNotification()
    {
        _notificationService.TestNotification();
    }

    [RelayCommand]
    private void UnsubscribeToTag(string tag)
    {
        _notificationService.UnsubscribeFromTag(tag);
        UpdateSubscriptions();
    }

    private void UpdateSubscriptions()
    {
        var currentSubscriptions = _notificationService.GetSubscriptions();
        
        Subscriptions.Clear();
        foreach (var subscription in currentSubscriptions)
        {
            Subscriptions.Add(subscription);
        }
    }

    private void OnNewQuestionDetected(object? sender, StackOverflowQuestion question)
    {
        // This could be handled by the main view model or passed through events
    }

    private void OnNotificationReceived(object? sender, Notification notification)
    {
        // This could be handled by the main view model or passed through events
    }
}