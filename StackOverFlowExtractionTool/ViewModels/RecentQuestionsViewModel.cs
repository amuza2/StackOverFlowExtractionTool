using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.Services;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class RecentQuestionsViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;
    public event EventHandler<string>? OnSubscriptionAdded;
    
    [ObservableProperty]
    private NotificationViewModel _notificationViewModel;
    
    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _recentQuestions = new();

    [ObservableProperty]
    private ObservableCollection<StackOverflowQuestion> _filteredRecentQuestions = new();

    [ObservableProperty]
    private ObservableCollection<FilterTag> _filterTags = new();

    [ObservableProperty]
    private ObservableCollection<TagSubscription> _subscriptions = new();

    [ObservableProperty]
    private FilterTag _selectedFilterTag;
    
    [ObservableProperty]
    private bool _isSubscriptionDropdownOpen;
    
    [ObservableProperty]
    private string _newSubscriptionTag = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<string> _subscriptionSuggestions = new();

    public RecentQuestionsViewModel(INotificationService notificationService, NotificationViewModel notificationViewModel)
    {
        _notificationService = notificationService;
        _notificationViewModel = notificationViewModel;

        var allTag = new FilterTag { Tag = "All", IsSelected = true };
        FilterTags.Add(allTag);
        _selectedFilterTag = allTag;
        
        _notificationService.SubscriptionChanged += OnSubscriptionChanged;
        UpdateSubscriptions();
        
        ApplyFilter();
    }

    public RecentQuestionsViewModel(NotificationViewModel notificationViewModel) : this(null!,null!){ }

    [RelayCommand]
    private void ToggleSubscriptionDropdown()
    {
        IsSubscriptionDropdownOpen = !IsSubscriptionDropdownOpen;
        if (IsSubscriptionDropdownOpen)
        {
            UpdateSubscriptionSuggestions();
        }
    }
    
    [RelayCommand]
    private void AddSubscription()
    {
        if (string.IsNullOrWhiteSpace(NewSubscriptionTag))
            return;

        var tag = NewSubscriptionTag.Trim();
        
        _notificationService.SubscribeToTag(tag);
        UpdateSubscriptions();
        _notificationService.StartMonitoringAsync();
        
        NewSubscriptionTag = string.Empty;
        IsSubscriptionDropdownOpen = false;
        
        // Optional: Show status message
        OnSubscriptionAdded?.Invoke(this, tag);
    }
    
    [RelayCommand]
    private void CancelSubscription()
    {
        NewSubscriptionTag = string.Empty;
        IsSubscriptionDropdownOpen = false;
    }
    private void UpdateSubscriptionSuggestions()
    {
        // Get suggestions from recent questions tags
        var suggestions = RecentQuestions
            .SelectMany(q => q.Tags)
            .Distinct()
            .Where(t => !Subscriptions.Any(s => s.Tag == t))
            .OrderBy(t => t)
            .Take(5)
            .ToList();

        SubscriptionSuggestions = new ObservableCollection<string>(suggestions);
    }

    [RelayCommand]
    private void UseSuggestion(string tag)
    {
        NewSubscriptionTag = tag;
    }
    
    public void AddQuestion(StackOverflowQuestion question)
    {
        // Remove existing question with same ID
        var existing = RecentQuestions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
        if (existing != null)
        {
            RecentQuestions.Remove(existing);
        }

        RecentQuestions.Insert(0, question);
        ApplyFilter();

        // Keep only recent 50 questions
        if (RecentQuestions.Count > 50)
        {
            RecentQuestions.RemoveAt(RecentQuestions.Count - 1);
        }
    }

    [RelayCommand]
    private void FilterByTag(string tag)
    {
        foreach (var filterTag in FilterTags)
        {
            filterTag.IsSelected = filterTag.Tag == tag;
            if (filterTag.IsSelected)
            {
                SelectedFilterTag = filterTag;
            }
        }
        SelectedFilterTag.Tag = tag;
        ApplyFilter();
    }

    [RelayCommand]
    private void RemoveFilterTag(string tag)
    {
        if (tag == "All") return;
        
        _notificationService.UnsubscribeFromTag(tag);
        UpdateSubscriptions();
        
        var tagToRemove = FilterTags.FirstOrDefault(t => t.Tag == tag);
        if (tagToRemove != null) FilterTags.Remove(tagToRemove);
        
        if (SelectedFilterTag.Tag == tag)
        {
            var allTag = FilterTags.FirstOrDefault(t => t.Tag == "All");
            if (allTag != null)
            {
                allTag.IsSelected = true;
                SelectedFilterTag.Tag = "All";
                ApplyFilter();
            }
        }
    }

    [RelayCommand]
    private void ClearRecentQuestions()
    {
        RecentQuestions.Clear();
        ApplyFilter();
    }

    [RelayCommand]
    private void OpenQuestionInBrowser(StackOverflowQuestion question)
    {
        try
        {
            var url = question.Link;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Could add status message handling here if needed
            System.Diagnostics.Debug.WriteLine($"Error opening browser: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CopyLink(string url)
    {
        try
        {
            await Extensions.ClipboardHelper.SetTextAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to copy link: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        if (SelectedFilterTag.Tag == "All")
        {
            FilteredRecentQuestions = new ObservableCollection<StackOverflowQuestion>(RecentQuestions);
        }
        else
        {
            var filtered = RecentQuestions
                .Where(q => q.Tags.Contains(SelectedFilterTag.Tag))
                .ToList();
            FilteredRecentQuestions = new ObservableCollection<StackOverflowQuestion>(filtered);
        }
    }

    private void UpdateSubscriptions()
    {
        Subscriptions = new ObservableCollection<TagSubscription>(_notificationService.GetSubscriptions());
        UpdateFilterTags();
    }

    private void UpdateFilterTags()
    {
        var currentTags = FilterTags.ToList();
        var currentlySelectedTag = SelectedFilterTag.Tag;
        
        FilterTags.Clear();
        
        // Always add "All" tag first
        var allTag = currentTags.FirstOrDefault(t => t.Tag == "All") ?? new FilterTag { Tag = "All" };
        allTag.IsSelected = (currentlySelectedTag == "All");
        FilterTags.Add(allTag);

        // Add subscribed tags
        foreach (var subscription in Subscriptions)
        {
            var existingTag = currentTags.FirstOrDefault(t => t.Tag == subscription.Tag);
            var filterTag = existingTag ?? new FilterTag { Tag = subscription.Tag };
            filterTag.IsSelected = (subscription.Tag == currentlySelectedTag);
            FilterTags.Add(filterTag);
        }
        
        // Update selected filter tag reference
        SelectedFilterTag = FilterTags.FirstOrDefault(t => t.IsSelected) ?? allTag;
    }

    private void OnSubscriptionChanged(object? sender, EventArgs e) => UpdateSubscriptions();
    
    [RelayCommand]
    private void UnsubscribeToTag(string tag)
    {
        _notificationService.UnsubscribeFromTag(tag);
        UpdateSubscriptions();
        Console.WriteLine($"Unsubscribed from tag: {tag}");
    }
    
    [RelayCommand]
    private void RemoveSubscription(string tag)
    {
        _notificationService.UnsubscribeFromTag(tag);
        UpdateSubscriptions();
        Console.WriteLine($"Removed from tag: {tag}");
    }

    [RelayCommand]
    private void MarkAllNotificationsAsRead()
    {
       _notificationViewModel.MarkAllAsReadCommand.Execute(null);
    }

    
    [RelayCommand]
    private void TestNotification()
    {
        _notificationService.TestNotification();
    }
}