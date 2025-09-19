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
    bool TogglePopupNotifications();
    void TestNotification();
}