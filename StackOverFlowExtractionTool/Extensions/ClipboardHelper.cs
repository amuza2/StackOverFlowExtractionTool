using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace StackOverFlowExtractionTool.Extensions;

public static class ClipboardHelper
{
    public static async Task<bool> SetTextAsync(string text)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(
                (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
                
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(text);
                return true;
            }
        }
        catch (Exception)
        {
            // Log error if needed
        }
        return false;
    }
}