using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StackOverFlowExtractionTool.Views;

public partial class NotificationWindow : Window
{
    public NotificationWindow()
    {
        InitializeComponent();
        
        #if DEBUG
                this.AttachDevTools();
        #endif
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        // Position window in bottom right corner
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            Position = new PixelPoint(
                workingArea.Right - (int)Width - 10,
                workingArea.Bottom - (int)Height - 10
            );
        }
    }
}