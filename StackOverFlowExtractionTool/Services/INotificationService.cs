using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackOverFlowExtractionTool.Models;

namespace StackOverFlowExtractionTool.Services;

public interface INotificationService
{
    void ShowNotification(StackOverflowQuestion question, string tag);
    void SubscribeToTag(string tag);
    void UnsubscribeFromTag(string tag);
    List<TagSubscription> GetSubscriptions();
    Task StartMonitoringAsync();
    void StopMonitoring();
    event EventHandler<Notification>? NotificationReceived;
    event EventHandler<StackOverflowQuestion>? NewQuestionDetected;
    public event EventHandler? SubscriptionChanged;
    // void SetNotificationSound(bool enable);
    void SetNotificationDuration(int seconds);
    bool GetPopupNotificationsEnabled();
    void TestNotification();
    void PlaySound();
    bool GetNotificationSoundEnabled();
    bool IsMonitoring { get; }
    void SetMonitoringInterval(int minutes);
}