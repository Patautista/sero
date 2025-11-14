using Business.Audio;
using MauiApp1.Services.Cache;
using System.Collections.Concurrent;

namespace MauiApp1.Services.Audio
{
    public class AudioCachePreloadService
    {
        private readonly IAudioCache _audioCache;
        private readonly ApiService _apiService;
        private readonly DatabaseService _databaseService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<AudioCacheRequest> _queue;
        private readonly SemaphoreSlim _semaphore;
        private Task? _backgroundTask;
        private bool _isRunning;

        public AudioCachePreloadService(
            IAudioCache audioCache,
            ApiService apiService,
            DatabaseService databaseService)
        {
            _audioCache = audioCache;
            _apiService = apiService;
            _databaseService = databaseService;
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
            _backgroundTask = Task.Run(async () => await PreloadAudioForDeckAsync(deckId, targetLanguageCode, _cancellationTokenSource.Token));
            
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

        private async Task PreloadAudioForDeckAsync(int deckId, string targetLanguageCode, CancellationToken cancellationToken)
        {
            try
            {
                // Check if API is available
                var isApiAvailable = await _apiService.IsAvailable();
                if (!isApiAvailable)
                {
                    Console.WriteLine("API is not available. Skipping audio preload.");
                    return;
                }

                // Get all cards from the deck
                var cards = await _databaseService.QuerySrsCardsAsync(deckId, cancellationToken: cancellationToken);
                
                if (cards == null || cards.Count == 0)
                {
                    Console.WriteLine($"No cards found in deck {deckId}");
                    return;
                }

                Console.WriteLine($"Found {cards.Count} cards to process for audio cache.");

                var processedCount = 0;
                var cachedCount = 0;
                var downloadedCount = 0;

                foreach (var card in cards)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Process target language sentences
                    foreach (var sentence in card.SentencesInTargetLanguage)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (string.IsNullOrWhiteSpace(sentence.Text))
                            continue;

                        // Check if any gender is already cached
                        var anyCached = false;
                        foreach (VoiceGender gender in Enum.GetValues(typeof(VoiceGender)))
                        {
                            var cacheKey = _audioCache.ComputeCacheKey(sentence.Text, targetLanguageCode, gender);
                            var cachedAudio = await _audioCache.GetBytesAsync(cacheKey);

                            if (cachedAudio != null)
                            {
                                cachedCount++;
                                anyCached = true;
                                break; // Cache hit - skip this sentence
                            }
                        }

                        if (anyCached)
                            continue; // Skip to next sentence

                        // Download and cache audio only for female voice
                        await _semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var audioData = await _apiService.GetTTSAsync(sentence.Text, targetLanguageCode, "female");

                            if (audioData != null && audioData.Length > 0)
                            {
                                var femaleCacheKey = _audioCache.ComputeCacheKey(sentence.Text, targetLanguageCode, VoiceGender.Female);
                                await _audioCache.SetAsync(femaleCacheKey, audioData);
                                downloadedCount++;
                                Console.WriteLine($"Cached audio for: {sentence.Text.Substring(0, Math.Min(30, sentence.Text.Length))}... (Female)");
                            }

                            // Add a small delay to avoid hammering the API
                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to cache audio for '{sentence.Text}': {ex.Message}");
                        }
                        finally
                        {
                            _semaphore.Release();
                        }

                        processedCount++;
                    }
                }

                Console.WriteLine($"Audio preloading completed. Processed: {processedCount}, Already cached: {cachedCount}, Downloaded: {downloadedCount}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Audio preloading was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during audio preloading: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
            }
        }

        private class AudioCacheRequest
        {
            public string Text { get; set; } = string.Empty;
            public string LanguageCode { get; set; } = string.Empty;
            public VoiceGender VoiceGender { get; set; }
        }
    }
}