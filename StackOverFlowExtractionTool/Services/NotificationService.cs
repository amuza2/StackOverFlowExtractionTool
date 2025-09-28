using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StackOverFlowExtractionTool.Models;
using StackOverFlowExtractionTool.ViewModels;
using Notification = StackOverFlowExtractionTool.Models.Notification;

namespace StackOverFlowExtractionTool.Services;

public partial class NotificationService : INotificationService
{
    private readonly IStackOverflowService _stackOverflowService;
    private readonly ILogger<NotificationService> _logger;
    private readonly List<TagSubscription> _subscriptions = new();
    private WindowNotificationManager? _notificationManager;
    private CancellationTokenSource? _monitoringCts;
    private readonly IAppSettingsService _appSettingsService;
    private bool _isMonitoring;
    private bool _enablePopupNotifications = true;
    private int _notificationDuration;
    private int _monitoringInterval = 1;
    public bool IsMonitoring => _isMonitoring;
    
    
    public event EventHandler<Notification>? NotificationReceived;
    public event EventHandler<StackOverflowQuestion>? NewQuestionDetected;
    public event EventHandler? SubscriptionChanged;

    public NotificationService(
        IStackOverflowService stackOverflowService,
        ILogger<NotificationService> logger, IAppSettingsService appSettingsService)
    {
        _stackOverflowService = stackOverflowService;
        _logger = logger;
        _appSettingsService = appSettingsService;
    }
    
    public bool GetPopupNotificationsEnabled()
    {
        return _appSettingsService?.GetSetting<bool>(nameof(SettingsViewModel.EnablePopupNotification), true) ?? true;
    }
    
    public void SetNotificationManager(WindowNotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    public void ShowNotification(StackOverflowQuestion question, string tag)
    {
        if (_notificationManager == null)
        {
            _logger.LogWarning("Notification manager not initialized");
            return;
        }
        
        try
        {
            var notification = new Notification
            {
                Title = $"New {tag} Question",
                Message = question.Title,
                Tag = tag,
                QuestionUrl = question.Link,
                QuestionId = question.QuestionId,
                Timestamp = DateTime.Now,
                
            };

            if (_enablePopupNotifications)
            {
                Console.WriteLine("Popup notifications Enabled");
                // Show OS system notification using notify-send
                ShowSystemNotification(notification.Title, notification.Message, notification.QuestionUrl);
            }

            // Raise event for UI notifications
            NotificationReceived?.Invoke(this, notification);
            
            _logger.LogInformation("Notification shown for question {QuestionId}", question.QuestionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification");
        }
    }

    private readonly ConcurrentDictionary<int, string> _activeNotifications = new();
    private void ShowSystemNotification(string title, string message, string url)
    {
        if (!GetPopupNotificationsEnabled())
        {
            Console.WriteLine("Popup notifications are disabled, skipping...");
            return;
        }

        if (GetNotificationSoundEnabled())
        {
            Console.WriteLine("Sound notification is enabled!");
            PlaySound();
        }
        
        try
        {
            var iconPath = String.Empty;
            var sourceDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            if (sourceDir != null)
            {
                iconPath = Path.Combine(sourceDir, "Assets", "stack-overflow.png");
            }
            var escapedTitle = title.Replace("\"", "\\\"");
            var escapedMessage = message.Replace("\"", "\\\"");
            
            _logger.LogInformation("Looking for icon at: {IconPath}", iconPath);
            _logger.LogInformation("Icon exists: {Exists}", File.Exists(iconPath));
        
            if (!File.Exists(iconPath))
            {
                _logger.LogWarning("Custom icon not found, using fallback");
            }

            // Use icon only if file exists, otherwise use a standard icon name
            var iconArg = File.Exists(iconPath) 
                ? $"--icon=\"{iconPath}\"" 
                : "--icon=dialog-information";
            
            // Convert seconds to milliseconds for notify-send
            var expireTime = _notificationDuration * 1000;
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"--app-name=\"StackOverflow\" " +
                            $"--urgency=normal " +
                            $"--expire-time={expireTime} " +
                            $"--category=im.received " +
                            $"{iconArg} " +
                            $"--hint=string:x-canonical-private-synchronous:stackoverflow " +
                            $"--action=default=Open " +
                            $"\"{escapedTitle}\" \"{escapedMessage}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(startInfo);

            if (process != null)
            {
                // Store the URL with the process ID
                _activeNotifications[process.Id] = url;

                // Handle click action for this specific process
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        // Only open if this specific notification was clicked
                        // and not if it just expired (process.ExitCode == 0 without output)
                        if (output.Trim() == "default" &&
                            _activeNotifications.TryRemove(process.Id, out string? storedUrl))
                        {
                            OpenUrl(storedUrl);
                            _logger.LogInformation("Opened URL from notification click: {Url}", storedUrl);
                        }
                        else
                        {
                            // Clean up if notification expired without being clicked
                            _activeNotifications.TryRemove(process.Id, out _);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling notification click for process {ProcessId}", process.Id);
                        _activeNotifications.TryRemove(process.Id, out _);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show system notification");
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL: {Url}", url);
        }
    }
    
    public void SubscribeToTag(string tag)
    {
        var cleanTag = tag.Trim().ToLower();
        var existing = _subscriptions.FirstOrDefault(s => s.Tag.Equals(cleanTag));
        
        if (existing == null)
        {
            _subscriptions.Add(new TagSubscription
            {
                Tag = cleanTag,
                LastChecked = DateTime.Now.AddMinutes(-5), // Check immediately
                IsActive = true
            });
            _logger.LogInformation("Subscribed to tag: {Tag}", cleanTag);
        }
        else
        {
            existing.IsActive = true;
            existing.LastChecked = DateTime.Now.AddMinutes(-5); // Reset to check immediately
            _logger.LogInformation("Reactivated subscription to tag: {Tag}", cleanTag);
        }
    }

    public void UnsubscribeFromTag(string tag)
    {
        var cleanTag = tag.Trim().ToLower();
        var subscription = _subscriptions.FirstOrDefault(s => s.Tag.Equals(cleanTag));
        if (subscription != null)
        {
            subscription.IsActive = false;
            _logger.LogInformation("Unsubscribed from tag: {Tag}", cleanTag);
        }
        
        var isActiveTag = _subscriptions.Any(s => s.IsActive);
        if(!isActiveTag && _isMonitoring)
            StopMonitoring();
    }
    
    public void SetMonitoringInterval(int minutes)
    {
        _monitoringInterval = Math.Clamp(minutes, 1, 60); // Limit between 1-60 minutes
        Console.WriteLine($"Monitoring interval set to {_monitoringInterval} minutes");
    }

    public List<TagSubscription> GetSubscriptions() => 
        _subscriptions.Where(s => s.IsActive).ToList();

    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        _monitoringCts = new CancellationTokenSource();

        _logger.LogInformation("Starting tag monitoring service");

        Console.WriteLine("=== MONITORING SERVICE STARTED ===");
        
        try
        {
            int cycle = 0;
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                cycle++;
                Console.WriteLine($"=== MONITORING CYCLE {cycle} at {DateTime.Now:HH:mm:ss} ===");
                
                await CheckForNewQuestionsAsync();
                await Task.Delay(TimeSpan.FromMinutes(_monitoringInterval), _monitoringCts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Monitoring service stopped gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitoring loop");
            Console.WriteLine($"Monitoring service error: {ex.Message}");
        }
        finally
        {
            _isMonitoring = false;
            Console.WriteLine("=== MONITORING SERVICE STOPPED ===");
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;
        
        _monitoringCts?.Cancel();
        _isMonitoring = false;
        _logger.LogInformation("Stopped tag monitoring service");
        Console.WriteLine("=== MONITORING SERVICE STOPPED 2 ===");
    }

    private async Task CheckForNewQuestionsAsync()
    {
        Console.WriteLine($"=== CHECKING FOR NEW QUESTIONS at {DateTime.Now:HH:mm:ss} ===");
        var activeSubscriptions = _subscriptions.Where(s => s.IsActive).ToList();
        
        Console.WriteLine($"Active subscriptions: {activeSubscriptions.Count}");
        
        foreach (var subscription in activeSubscriptions)
        {
            Console.WriteLine($"Checking tag: {subscription.Tag}");
            Console.WriteLine($"Last checked: {subscription.LastChecked:HH:mm:ss}");
            Console.WriteLine($"Last question ID: {subscription.LastQuestionId}");

            if (DateTime.Now - subscription.LastChecked < subscription.CheckInterval)
            {
                Console.WriteLine($"Skipping {subscription.Tag} - not time to check yet");
                continue;
            }

            try
            {
                Console.WriteLine($"Fetching questions for {subscription.Tag}...");
                var questions = await _stackOverflowService.GetRecentQuestionsByTagAsync(
                    subscription.Tag, 1, 5);

                Console.WriteLine($"Found {questions.Count} questions total");
                
                var newQuestions = questions
                    .Where(q => q.QuestionId > subscription.LastQuestionId || 
                                q.CreationDateTime > subscription.LastChecked.AddMinutes(-5))
                    .OrderBy(q => q.QuestionId)
                    .ToList();
                
                Console.WriteLine($"Found {newQuestions.Count} new questions (ID > {subscription.LastQuestionId})");

                if (newQuestions.Any())
                {
                    foreach (var question in newQuestions)
                    {
                        Console.WriteLine($"New question: #{question.QuestionId} - {question.Title}");
                        ShowNotification(question, subscription.Tag);
                        NewQuestionDetected?.Invoke(this, question);
                    }

                    subscription.LastQuestionId = newQuestions.Max(q => q.QuestionId);
                    Console.WriteLine($"Updated last question ID to: {subscription.LastQuestionId}");
                    _logger.LogInformation("Found {Count} new questions for tag: {Tag}", 
                        newQuestions.Count, subscription.Tag);
                }
                else
                {
                    Console.WriteLine($"No new questions found for {subscription.Tag}");
                }

                subscription.LastChecked = DateTime.Now;
                Console.WriteLine($"Updated last checked time to: {subscription.LastChecked:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for new questions for tag: {Tag}", subscription.Tag);
                Console.WriteLine($"ERROR checking {subscription.Tag}: {ex.Message}");
            }
            Console.WriteLine("---");
        }
        Console.WriteLine("=== CHECK COMPLETE ===");
    }

    public bool GetNotificationSoundEnabled()
    {
        return _appSettingsService?.GetSetting<bool>(nameof(SettingsViewModel.EnableNotificationSound), true) ?? true;
    }
    
    public void PlaySound()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "beep",
                Arguments = "-f 1000 -l 300",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo)?.WaitForExit(500);
        }
        catch
        {
            try
            {
                // Fallback to console beep
                Console.Beep(1000, 300);
            }
            catch
            {
                // Final fallback
                Console.WriteLine("🔔 Test sound!");
            }
        }
    }

    public void SetNotificationDuration(int seconds)
    {
        _notificationDuration = Math.Clamp(seconds, 2, 30);
        _logger.LogInformation("Notification duration set to {Seconds} seconds", _notificationDuration);
    }
    
    public void TestNotification()
    {
        var testQuestion = new StackOverflowQuestion
        {
            QuestionId = 999,
            Title = "Test Notification - Is this working?",
            Link = "https://stackoverflow.com/questions/12345678",
            Score = 5,
            AnswerCount = 2,
            IsAnswered = true,
            ViewCount = 100,
            CreationDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            LastActivityDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Tags = new List<string> { "test" }
        };

        ShowNotification(testQuestion, "test");
        PlaySound();
    }
}