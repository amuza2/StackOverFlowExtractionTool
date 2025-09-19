using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using StackOverFlowExtractionTool.ViewModels;

namespace StackOverFlowExtractionTool.Models;

public partial class Notification : ViewModelBase
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private DateTime _timestamp = DateTime.Now;
    [ObservableProperty] private bool _isUnread = true;
    
    // public StackOverflowQuestion? Question { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string QuestionUrl { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    
    public string FormattedTime => Timestamp.ToString("HH:mm:ss");
    public string TimeAgo => GetTimeAgo();
    
    private string GetTimeAgo()
    {
        var timeSpan = DateTime.Now - Timestamp;
        return timeSpan.TotalSeconds < 60 ? "Just now" :
            timeSpan.TotalMinutes < 60 ? $"{(int)timeSpan.TotalMinutes}m ago" :
            timeSpan.TotalHours < 24 ? $"{(int)timeSpan.TotalHours}h ago" :
            $"{(int)timeSpan.TotalDays}d ago";
    }
}