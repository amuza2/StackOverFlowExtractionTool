using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackOverFlowExtractionTool.Extensions;
using StackOverFlowExtractionTool.Services;
using StackOverFlowExtractionTool.ViewModels;
using StackOverFlowExtractionTool.Views;

namespace StackOverFlowExtractionTool;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        var collection = new ServiceCollection();
        
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        
        // Logging
        collection.AddLogging(builder =>
        {
            builder.AddProvider(new FileLoggerProvider("logs/app.log"));
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        collection.AddHttpClient<StackOverflowService>(client =>
        {
            client.BaseAddress = new Uri("https://api.stackexchange.com/2.3/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "StackOverflowExtractor/1.0");
        });
        
        collection.AddSingleton<ICacheService, CacheService>();
        collection.AddSingleton<StackOverflowService>();
        collection.AddSingleton<IStackOverflowService>(provider =>
        {
            var originalService = provider.GetRequiredService<StackOverflowService>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedStackOverflowService>>();
            return new CachedStackOverflowService(originalService, cacheService, logger);
        });
        
        collection.AddSingleton<INotificationService, NotificationService>();
        collection.AddSingleton<NotificationWindow>();
        collection.AddSingleton<NotificationViewModel>();
        
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<SearchTabViewModel>();
        collection.AddTransient<RecentQuestionsViewModel>();
        collection.AddTransient<SettingsViewModel>();
        
        var services = collection.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = vm,
            };
            desktop.MainWindow = mainWindow;
            
            // WindowNotificationManager
            var notificationManager = new WindowNotificationManager(mainWindow)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3
            };
            
            var notificationService = services.GetRequiredService<INotificationService>();
            if (notificationService is NotificationService concreteNotificationService)
            {
                // You'll need to add a method to set the notification manager
                concreteNotificationService.SetNotificationManager(notificationManager);
            }

        }

        base.OnFrameworkInitializationCompleted();
    }
}