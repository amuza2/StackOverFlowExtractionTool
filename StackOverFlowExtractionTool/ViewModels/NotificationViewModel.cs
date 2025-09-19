using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq; 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StackOverFlowExtractionTool.Models;

namespace StackOverFlowExtractionTool.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Notification> _notifications = new();

    [ObservableProperty]
    private int _unreadCount;

    [RelayCommand]
    private void OpenNotification(Notification notification)
    {
        try
        {
            notification.IsUnread = false;
            UpdateUnreadCount();
            
            Process.Start(new ProcessStartInfo
            {
                FileName = notification.QuestionUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    [RelayCommand]
    private void MarkAllAsRead()
    {
        foreach (var notification in Notifications)
        {
            notification.IsUnread = false;
        }
        UpdateUnreadCount();
    }

    [RelayCommand]
    private void ClearAll()
    {
        Notifications.Clear();
        UpdateUnreadCount();
    }

    public void AddNotification(Notification notification)
    {
        Notifications.Insert(0, notification);
        UpdateUnreadCount();
        
        // Keep only last 50 notifications
        if (Notifications.Count > 50)
        {
            Notifications.RemoveAt(Notifications.Count - 1);
        }
    }

    private void UpdateUnreadCount()
    {
        UnreadCount = Notifications.Where(n => n.IsUnread).ToList().Count;
    }
}