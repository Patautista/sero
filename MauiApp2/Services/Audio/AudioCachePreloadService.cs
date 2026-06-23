using Business.Audio;
using MauiApp1.Services.Cache;
using MauiApp2.Services;
using System.Collections.Concurrent;

namespace MauiApp1.Services.Audio
{
    public class AudioCachePreloadService
    {
        private readonly IAudioCache _audioCache;
        private readonly IApiService _apiService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<AudioCacheRequest> _queue;
        private readonly SemaphoreSlim _semaphore;
        private Task? _backgroundTask;
        private bool _isRunning;

        public AudioCachePreloadService(
            IAudioCache audioCache,
            IApiService apiService)
        {
            _audioCache = audioCache;
            _apiService = apiService;
            _cancellationTokenSource = new CancellationTokenSource();
            _queue = new ConcurrentQueue<AudioCacheRequest>();
            _semaphore = new SemaphoreSlim(1, 1); // Limit to 1 concurrent request to avoid overwhelming the API
        }

        public async Task StartPreloadingAsync(int deckId, string targetLanguageCode)
        {
            if (_isRunning)
            {
                Console.WriteLine("Audio preload already running.");
                return;
            }

            _isRunning = true;
            //_backgroundTask = Task.Run(async () => await PreloadAudioForDeckAsync(deckId, targetLanguageCode, _cancellationTokenSource.Token));
            
            Console.WriteLine($"Started audio preloading for deck {deckId}");
        }

        public async Task StopPreloadingAsync()
        {
            if (!_isRunning)
                return;

            _cancellationTokenSource.Cancel();
            
            if (_backgroundTask != null)
                await _backgroundTask;

            _isRunning = false;
            Console.WriteLine("Stopped audio preloading.");
        }

        

        private class AudioCacheRequest
        {
            public string Text { get; set; } = string.Empty;
            public string LanguageCode { get; set; } = string.Empty;
            public VoiceGender VoiceGender { get; set; }
        }
    }
}