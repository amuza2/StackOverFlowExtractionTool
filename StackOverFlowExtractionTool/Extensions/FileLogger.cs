using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StackOverFlowExtractionTool.Extensions;

    public class FileLogger : ILogger, IDisposable
{
    private readonly string _filePath;
    private readonly string _categoryName;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed = false;

    public FileLogger(string filePath, string categoryName)
    {
        _filePath = filePath;
        _categoryName = categoryName;
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || _disposed)
            return;

        try
        {
            var message = FormatMessage(logLevel, eventId, state, exception, formatter);
            WriteToFileAsync(message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FileLogger error: {ex.Message}");
        }
    }
    private async Task WriteToFileAsync(string message)
    {
        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_filePath, message + Environment.NewLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private string FormatMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = formatter(state, exception);
        var exceptionInfo = exception != null ? $"\nException: {exception}" : "";
        
        return $"{timestamp} [{logLevel}] {_categoryName}: {formattedMessage}{exceptionInfo}";
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}