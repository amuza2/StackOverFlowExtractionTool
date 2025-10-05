using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
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
    private IServiceProvider? _services;
    private MainWindow? _mainWindow;
    private NativeMenuItem? _toggleMenuItem;
    private string _exitIcon = "icons8-exit-24.png";
    private string _stackoverflowIcon = "stack-overflow.png";
    private string _githubIcon = "icons8-github-24.png";
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
        collection.AddSingleton<IAppSettingsService, AppSettingsService>();
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
            _mainWindow = new MainWindow
            {
                DataContext = vm,
            };
            desktop.MainWindow = _mainWindow;
            
            // WindowNotificationManager
            var notificationManager = new WindowNotificationManager(_mainWindow)
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
            
            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            _mainWindow.Closing += OnMainWindowClosing;
            
            CreateTrayIcon();

            if(_toggleMenuItem != null)
                _toggleMenuItem.Header = "Hide";

        }

        base.OnFrameworkInitializationCompleted();
    }

    // Hide to tray instead of closing
    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        var logger = _services?.GetService<ILogger<App>>();
        e.Cancel = true;
        _mainWindow?.Hide();
        UpdateToggleMenuText();
        logger?.LogInformation("Window hidden to system tray");
    }

    private void OnOpenOrHideClicked(object? sender, EventArgs e)
    {
        ToggleWindow();
        UpdateToggleMenuText();
    }
    private void UpdateToggleMenuText()
    {
        if (_toggleMenuItem != null)
        {
            _toggleMenuItem.Header = GetToggleMenuText();
        }
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
    
    private void ToggleWindow()
    {
        if (_mainWindow == null) return;
    
        var logger = _services?.GetService<ILogger<App>>();
    
        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
            logger?.LogInformation("Window hidden to tray");
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            logger?.LogInformation("Window shown from tray");
        }
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ToggleWindow();
        UpdateToggleMenuText();
    }
    
    private void OnRepositoryClicked(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/amuza2/StackOverFlowExtractionTool",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            var logger = _services?.GetService<ILogger<App>>();
            logger?.LogError(ex, "Error opening repository URL");
        }
    }
    
   private void CreateTrayIcon()
{
    try
    {
        var statusMenuItem = new NativeMenuItem
        {
            Header = "Get Questions",
            IsEnabled = false,
            
        };
        
        _toggleMenuItem = new NativeMenuItem
        {
            Header = GetToggleMenuText()
        };
        _toggleMenuItem.Click += OnOpenOrHideClicked;

        var closeMenuItem = new NativeMenuItem
        {
            Header = "Exit",
            Icon = LoadBitmap($"/Assets/{_exitIcon}")
        };
        closeMenuItem.Click += OnExitClicked;

        var repositoryMenuItem = new NativeMenuItem
        {
            Header = "Go to Repository",
            Icon = LoadBitmap($"/Assets/{_githubIcon}")
        };

        repositoryMenuItem.Click += OnRepositoryClicked;
        
        // Create the native menu
        var menu = new NativeMenu();
        menu.Add(statusMenuItem);
        menu.Add(_toggleMenuItem);
        menu.Add(repositoryMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(closeMenuItem);

        // Create main tray icon
        var trayIcon = new TrayIcon
        {
            Icon = LoadWindowIcon($"/Assets/{_stackoverflowIcon}"),
            ToolTipText = "Stackoverflow Questions Extractor",
            Menu = menu
        };
        
        trayIcon.Clicked += OnTrayIconClicked;

        // Set the tray icon on the application
        var trayIcons = new TrayIcons { trayIcon };
        TrayIcon.SetIcons(this, trayIcons);

        var logger = _services?.GetService<ILogger<App>>();
        logger?.LogInformation("Tray icon created successfully");
    }
    catch (Exception ex)
    {
        var logger = _services?.GetService<ILogger<App>>();
        logger?.LogError(ex, "Error creating tray icon");
    }
}
private string GetToggleMenuText()
{
    return (_mainWindow?.IsVisible == true) ? "Hide" : "Open";
}

private Avalonia.Media.Imaging.Bitmap? LoadBitmap(string path)
{
    try
    {
        var uri = new Uri($"avares://StackOverFlowExtractionTool{path}");
        using var stream = Avalonia.Platform.AssetLoader.Open(uri);
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }
    catch (Exception ex)
    {
        var logger = _services?.GetService<ILogger<App>>();
        logger?.LogError(ex, "Error loading bitmap: {Path}", path);
        return null;
    }
}

private WindowIcon? LoadWindowIcon(string path)
{
    try
    {
        var uri = new Uri($"avares://StackOverFlowExtractionTool{path}");
        using var stream = Avalonia.Platform.AssetLoader.Open(uri);
        return new WindowIcon(stream);
    }
    catch (Exception ex)
    {
        var logger = _services?.GetService<ILogger<App>>();
        logger?.LogError(ex, "Error loading window icon: {Path}", path);
        return null;
    }
}
}