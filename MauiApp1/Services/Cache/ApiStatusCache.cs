namespace MauiApp1.Services.Cache
{
    public class ApiStatusCache
    {
        private bool? _cachedStatus;
        private DateTime? _cacheExpiry;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<bool?> GetCachedStatusAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_cachedStatus.HasValue && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
                {
                    return _cachedStatus;
                }
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetStatusAsync(bool status)
        {
            await _lock.WaitAsync();
            try
            {
                _cachedStatus = status;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task InvalidateCacheAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _cachedStatus = null;
                _cacheExpiry = null;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}