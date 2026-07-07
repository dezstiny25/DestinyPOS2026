using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Thread-safe file access layer that handles concurrent Excel file operations
/// Prevents file corruption by using exclusive file locks and retry logic
/// </summary>
public static class FileAccessLayer
{
    private static readonly object _lockInventory = new();
    private static readonly object _lockSalesReport = new();
    private static readonly int _maxRetries = 5;
    private static readonly int _retryDelayMs = 100;

    /// <summary>
    /// Acquires an exclusive lock on the Inventory file and executes the action
    /// </summary>
    public static void WithInventoryLock(Action action)
    {
        lock (_lockInventory)
        {
            ExecuteWithRetry(action);
        }
    }

    /// <summary>
    /// Acquires an exclusive lock on the Inventory file and executes the function
    /// </summary>
    public static T WithInventoryLock<T>(Func<T> func)
    {
        lock (_lockInventory)
        {
            return ExecuteWithRetry(func);
        }
    }

    /// <summary>
    /// Acquires an exclusive lock on the SalesReport file and executes the action
    /// </summary>
    public static void WithSalesReportLock(Action action)
    {
        lock (_lockSalesReport)
        {
            ExecuteWithRetry(action);
        }
    }

    /// <summary>
    /// Acquires an exclusive lock on the SalesReport file and executes the function
    /// </summary>
    public static T WithSalesReportLock<T>(Func<T> func)
    {
        lock (_lockSalesReport)
        {
            return ExecuteWithRetry(func);
        }
    }

    /// <summary>
    /// Waits for a file to be released (not in use)
    /// Useful before critical operations
    /// </summary>
    public static bool WaitForFileRelease(string filePath, int timeoutMs = 5000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        return false;
    }

    /// <summary>
    /// Executes an action with retry logic to handle file access conflicts
    /// </summary>
    private static void ExecuteWithRetry(Action action)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (IOException) when (attempt < _maxRetries - 1)
            {
                Thread.Sleep(_retryDelayMs * (attempt + 1));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"File access error after {_maxRetries} retries: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Executes a function with retry logic to handle file access conflicts
    /// </summary>
    private static T ExecuteWithRetry<T>(Func<T> func)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                return func();
            }
            catch (IOException) when (attempt < _maxRetries - 1)
            {
                Thread.Sleep(_retryDelayMs * (attempt + 1));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"File access error after {_maxRetries} retries: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("Failed to complete file operation after all retries");
    }
}
