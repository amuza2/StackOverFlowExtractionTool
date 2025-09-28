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
    private readonly IAppSettingsService _appSettingsService;
    private readonly ICacheService _cacheService;
    
    private bool _isUserInteraction = true;
    
    // Notification Settings
    [ObservableProperty]
    private bool _enablePopupNotification = true;
    
    [ObservableProperty]
    private bool _enableNotificationSound = true;
    
    [ObservableProperty]
    private int _notificationDuration = 10; // seconds
    
    // Monitoring Settings
    [ObservableProperty]
    private bool _isMonitoring;
    
    [ObservableProperty]
    private bool _autoStartMonitoring;
    
    [ObservableProperty]
    private string _monitoringStatus = "Monitoring: Off";
    
    // Data Management Settings
    [ObservableProperty]
    private int _maxRecentQuestions = 50;
    
    [ObservableProperty]
    private int _cacheExpirationHours = 24;
    
    // Subscriptions & Cache
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

    [ObservableProperty] private int _monitoringInterval = 1;

    // Display properties for UI
    public string NotificationPopupState => EnablePopupNotification ? "🔔 Pop-up Enabled" : "🔕 Pop-up Disabled";
    public string BellIcon => EnablePopupNotification ? "🔔" : "🔕";
    public string SoundIcon => EnableNotificationSound ? "🔊" : "🔇";
    public string MonitoringIcon => IsMonitoring ? "🔵" : "🔴";
    
    

    public SettingsViewModel(INotificationService notificationService, IAppSettingsService appSettingsService, ICacheService cacheService)
    {
        _notificationService = notificationService;
        _appSettingsService = appSettingsService;
        _cacheService = cacheService;

        // Load settings when ViewModel is created
        LoadSettings();
        
        // Subscribe to notification events
        _notificationService.NotificationReceived += OnNotificationReceived;
        _notificationService.NewQuestionDetected += OnNewQuestionDetected;
        _notificationService.SubscriptionChanged += OnSubscriptionChanged;
        
        UpdateSubscriptions();
        UpdateCacheStatistics();
    }

    public SettingsViewModel(ICacheService cacheService) : this(null!, null!, cacheService) { }

    private void LoadSettings()
    {
        // Load from settings service or use defaults
        EnablePopupNotification = _appSettingsService?.GetSetting(nameof(EnablePopupNotification), true) ?? true;
        AutoStartMonitoring = _appSettingsService?.GetSetting(nameof(AutoStartMonitoring), false) ?? false;
        EnableNotificationSound = _appSettingsService?.GetSetting(nameof(EnableNotificationSound), true) ?? true;
        NotificationDuration = _appSettingsService?.GetSetting(nameof(NotificationDuration), 10) ?? 10;
        MaxRecentQuestions = _appSettingsService?.GetSetting(nameof(MaxRecentQuestions), 50) ?? 50;
        CacheExpirationHours = _appSettingsService?.GetSetting(nameof(CacheExpirationHours), 24) ?? 24;
        
        // Apply settings to notification service
        if (_notificationService != null)
        {
            _notificationService.SetNotificationDuration(NotificationDuration);
            _notificationService.SetMonitoringInterval(MonitoringInterval);
        }
        
        UpdateDisplayProperties();
    }

    private void SaveSettings()
    {
        _appSettingsService?.SetSetting(nameof(EnablePopupNotification), EnablePopupNotification);
        _appSettingsService?.SetSetting(nameof(AutoStartMonitoring), AutoStartMonitoring);
        _appSettingsService?.SetSetting(nameof(EnableNotificationSound), EnableNotificationSound);
        _appSettingsService?.SetSetting(nameof(NotificationDuration), NotificationDuration);
        _appSettingsService?.SetSetting(nameof(MaxRecentQuestions), MaxRecentQuestions);
        _appSettingsService?.SetSetting(nameof(CacheExpirationHours), CacheExpirationHours);
        _appSettingsService?.SetSetting(nameof(MonitoringInterval), MonitoringInterval);
        
        _appSettingsService?.Save();
    }
    
    partial void OnMonitoringIntervalChanged(int value)
    {
        SaveSettings();
        _notificationService?.SetMonitoringInterval(value);
    }

    private void UpdateDisplayProperties()
    {
        OnPropertyChanged(nameof(NotificationPopupState));
        OnPropertyChanged(nameof(BellIcon));
        OnPropertyChanged(nameof(SoundIcon));
        OnPropertyChanged(nameof(MonitoringIcon));
    }

    // Property change handlers
    partial void OnEnablePopupNotificationChanged(bool value)
    {
        SaveSettings();
        UpdateDisplayProperties();
        
        // Log the change
        if (value)
        {
            Console.WriteLine("==> Popup notifications: Enabled");
        }
        else
        {
            Console.WriteLine("==> Popup notifications: Disabled");
        }
    }

    partial void OnAutoStartMonitoringChanged(bool value)
    {
        SaveSettings();
    }

    partial void OnEnableNotificationSoundChanged(bool value)
    {
        SaveSettings();
        UpdateDisplayProperties();
        
        // Log the change
        if (value)
        {
            Console.WriteLine("==> Sound notifications: Enabled");
        }
        else
        {
            Console.WriteLine("==> Sound notifications: Disabled");
        }
    }

    partial void OnNotificationDurationChanged(int value)
    {
        SaveSettings();
        _notificationService?.SetNotificationDuration(value);
    }

    partial void OnMaxRecentQuestionsChanged(int value)
    {
        SaveSettings();
        // This will be used by RecentQuestionsViewModel
    }

    partial void OnCacheExpirationHoursChanged(int value)
    {
        SaveSettings();
        // This could be used by CacheService if you implement expiration
    }
    
    [RelayCommand]
    private void ToggleNotificationPopup()
    {
        EnablePopupNotification = !EnablePopupNotification;
    }

    [RelayCommand]
    private void ToggleNotificationSound()
    {
        EnableNotificationSound = !EnableNotificationSound;
    }

    // partial void OnIsMonitoringChanged(bool value)
    // {
    //     if(_isUserInteraction)
    //         ToggleMonitoringCommand.ExecuteAsync(null);
    // }

    [RelayCommand]
    private async Task ToggleMonitoring()
    {
        Console.WriteLine($"=== ToggleMonitoring Command Called ===");
        Console.WriteLine($"Service.IsMonitoring: {_notificationService.IsMonitoring}");
        Console.WriteLine($"ViewModel.IsMonitoring: {IsMonitoring}");
        
        _isUserInteraction = false;

        try
        {
            if (_notificationService.IsMonitoring)
            {
                _notificationService.StopMonitoring();
                IsMonitoring = false;
                MonitoringStatus = "Monitoring: Off";
                Console.WriteLine("Monitoring: Off");
            }
            else
            {
                _ =  _notificationService.StartMonitoringAsync();
                IsMonitoring = true;
                MonitoringStatus = "Monitoring: On";
                Console.WriteLine("Monitoring: On");
            }

            Console.WriteLine($"After toggle - Service.IsMonitoring: {_notificationService.IsMonitoring}");
            Console.WriteLine($"After toggle - ViewModel.IsMonitoring: {IsMonitoring}");
            Console.WriteLine($"=== ToggleMonitoring Command Completed ===");
            
            UpdateDisplayProperties();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _isUserInteraction = true;
        }
    }

    [RelayCommand]
    private void TestNotification()
    {
        _notificationService.TestNotification();
    }
    

    [RelayCommand]
    private void TestSound()
    {
        if (!EnableNotificationSound) return;

        _notificationService.PlaySound();
    }

    [RelayCommand]
    private void UnsubscribeFromTag(string tag)
    {
        _notificationService.UnsubscribeFromTag(tag);
        UpdateSubscriptions();
    }

    [RelayCommand]
    private void ClearAllSubscriptions()
    {
        foreach (var subscription in Subscriptions.ToList())
        {
            _notificationService.UnsubscribeFromTag(subscription.Tag);
        }
        UpdateSubscriptions();
    }

    [RelayCommand]
    private void ClearCache()
    {
        _cacheService.Clear();
        CacheStatus = "Cache: Cleared";
        UpdateCacheStatistics();
    }

    [RelayCommand]
    private async Task StartMonitoringOnLoad()
    {
        if (AutoStartMonitoring && !IsMonitoring)
        {
            await ToggleMonitoring();
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        EnablePopupNotification = true;
        EnableNotificationSound = true;
        NotificationDuration = 10;
        AutoStartMonitoring = false;
        MaxRecentQuestions = 50;
        CacheExpirationHours = 24;
        MonitoringInterval = 1;
        
        SaveSettings();
        UpdateDisplayProperties();
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

    private void UpdateCacheStatistics()
    {
        TotalCacheRequests = _cacheService.GetStats().Hits + _cacheService.GetStats().Misses;
        CacheStatus = $"Cache: {_cacheService.GetStats().Hits} hits, {_cacheService.GetStats().Misses} misses";
    }

    private void OnSubscriptionChanged(object? sender, EventArgs e)
    {
        UpdateSubscriptions();
    }

    private void OnNewQuestionDetected(object? sender, StackOverflowQuestion question)
    {
        // to be done later
    }

    private void OnNotificationReceived(object? sender, Notification notification)
    {
        // to be done later
    }
    
    [RelayCommand]
    private void IncreaseMonitoringInterval()
    {
        if (MonitoringInterval < 60)
            MonitoringInterval++;
    }

    [RelayCommand]
    private void DecreaseMonitoringInterval()
    {
        if (MonitoringInterval > 1)
            MonitoringInterval--;
    }
}