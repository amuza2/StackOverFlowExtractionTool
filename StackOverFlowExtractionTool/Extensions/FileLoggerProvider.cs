using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace StackOverFlowExtractionTool.Extensions;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private bool _disposed = false;

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
            _loggers.Clear();
            _disposed = true;
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileLoggerProvider));

        return _loggers.GetOrAdd(categoryName, name => new FileLogger(_filePath, name));

    }
}