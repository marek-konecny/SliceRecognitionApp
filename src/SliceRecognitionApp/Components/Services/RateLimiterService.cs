using System.Collections.Concurrent;

namespace SliceRecognitionApp.Components.Services;

public class RateLimiterService
{
    private readonly ConcurrentDictionary<string, DateTime> _requests = new();
    private readonly int _limitSeconds;

    public RateLimiterService(int limitSeconds = 2)
    {
        _limitSeconds = limitSeconds;
    }

    public bool IsRequestAllowed(string key)
    {
        // Clean up old entries occasionally
        CleanupOldEntries();

        var now = DateTime.UtcNow;

        if (_requests.TryGetValue(key, out var lastRequestTime))
        {
            if ((now - lastRequestTime).TotalSeconds < _limitSeconds)
            {
                return false; // Not allowed, too soon
            }
        }
        
        _requests[key] = now; // Update or add the new request time
        return true; // Allowed
    }

    private void CleanupOldEntries()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-_limitSeconds * 2);
        var oldKeys = _requests.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
        foreach (var key in oldKeys)
        {
            _requests.TryRemove(key, out _);
        }
    }
}