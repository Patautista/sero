using Infrastructure.Interfaces;
using Infrastructure.Vocab.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Vocab
{
    public class TatoebaConfig
    {
        /// <summary>
        /// Base URL for Tatoeba API
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.dev.tatoeba.org/unstable";

        /// <summary>
        /// Default target language code (ISO 639-3, e.g., "eng", "por", "nob")
        /// </summary>
        public string? TargetLanguageCode { get; set; }

        /// <summary>
        /// Default page size for search results
        /// </summary>
        public int DefaultPageSize { get; set; } = 10;

        /// <summary>
        /// Default exact search setting
        /// </summary>
        public bool DefaultExactSearch { get; set; } = true;
    }

    public class TatoebaApiClient : IExampleProvider, IDisposable
    {
        private readonly HttpClient _http;
        private readonly TatoebaConfig _config;

        public string ProviderName => "Tatoeba";
        public string LanguageCode => _config.TargetLanguageCode ?? "all"; // Supports all languages
        public bool SupportsAllLanguages => true;
        public bool SupportsSearch => true;
        public bool SupportsExactMatch => true;
        public bool SupportsTranslations => false; // Tatoeba has translations but requires separate API calls

        public TatoebaApiClient(TatoebaConfig? config = null)
        {
            _config = config ?? new TatoebaConfig();
            _http = new HttpClient { BaseAddress = new Uri(_config.BaseUrl) };
        }

        // Backward compatibility constructor
        public TatoebaApiClient(string baseUrl = "https://api.dev.tatoeba.org/unstable")
            : this(new TatoebaConfig { BaseUrl = baseUrl })
        {
        }

        public void Dispose()
        {
            _http?.Dispose();
        }

        #region IExampleProvider Implementation

        public async Task<ExampleSearchResult> SearchExamplesAsync(
            string query, 
            string? languageCode = null, 
            bool exactMatch = false, 
            int maxResults = 10)
        {
            // Use provided language code or fall back to configured default
            var targetLanguage = languageCode ?? _config.TargetLanguageCode;

            var tatoebaResult = await SearchSentencesAsync(
                language: targetLanguage,
                query: query,
                exactSearch: exactMatch,
                pageSize: maxResults);

            return new ExampleSearchResult
            {
                Query = query,
                LanguageCode = targetLanguage ?? "all",
                TotalResults = tatoebaResult?.Total ?? 0,
                Page = tatoebaResult?.Page ?? 1,
                PageSize = tatoebaResult?.PageSize ?? maxResults,
                Examples = tatoebaResult?.Data?.Select(s => new ExampleSentence
                {
                    Id = s.Id.ToString(),
                    Text = s.Text,
                    Language = s.Language,
                    Source = "Tatoeba",
                    Metadata = new Dictionary<string, object>
                    {
                        { "meaning_id", s.Id }
                    }
                }).ToList() ?? new List<ExampleSentence>()
            };
        }

        public async Task<List<ExampleSentence>> GetExamplesForWordAsync(string word, string? languageCode = null)
        {
            // Use provided language code or fall back to configured default
            var targetLanguage = languageCode ?? _config.TargetLanguageCode;

            // For Tatoeba, this is the same as searching with exact match
            var result = await SearchExamplesAsync(word, targetLanguage, exactMatch: _config.DefaultExactSearch);
            return result.Examples.ToList();
        }

        #endregion

        #region Tatoeba-Specific Methods

        /// <summary>
        /// Obtém a lista de servidores da API.
        /// </summary>
        public async Task<List<ServerInfo>> GetServersAsync()
        {
            var servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "Dev", Url = "https://api.dev.tatoeba.org" }
            };
            return await Task.FromResult(servers);
        }

        /// <summary>
        /// Busca sentenças com filtros opcionais.
        /// </summary>
        /// <param name="language">Código ISO da língua (ex: "eng", "por"). Se null, usa o idioma configurado.</param>
        /// <param name="query">Texto a buscar dentro da sentença.</param>
        /// <param name="exactSearch">Se true, busca por correspondência exata.</param>
        /// <param name="page">Página (opcional).</param>
        /// <param name="pageSize">Itens por página (opcional).</param>
        public async Task<SentenceSearchResult> SearchSentencesAsync(
            string? language = null, 
            string? query = null, 
            bool? exactSearch = null,
            int page = 1,
            int? pageSize = null)
        {
            // Use provided values or fall back to configured defaults
            var targetLanguage = language ?? _config.TargetLanguageCode;
            var useExactSearch = exactSearch ?? _config.DefaultExactSearch;
            var resultsPerPage = pageSize ?? _config.DefaultPageSize;

            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(targetLanguage)) 
                queryParams.Add($"lang={Uri.EscapeDataString(targetLanguage)}");
            
            if (!string.IsNullOrEmpty(query))
            {
                var searchQuery = useExactSearch 
                    ? $"={Uri.EscapeDataString(query)}" 
                    : Uri.EscapeDataString(query);
                queryParams.Add($"q={searchQuery}");
            }
            
            queryParams.Add("sort=relevance");
            //queryParams.Add($"page={page}");
            //queryParams.Add($"pageSize={resultsPerPage}");

            var url = "unstable/sentences";
            if (queryParams.Count > 0) 
                url += "?" + string.Join("&", queryParams);

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SentenceSearchResult>();
            return result;
        }

        /// <summary>
        /// Obtém uma sentença por ID.
        /// </summary>
        public async Task<Sentence> GetSentenceByIdAsync(int id)
        {
            var response = await _http.GetAsync($"sentences/{id}");
            response.EnsureSuccessStatusCode();
            var sentence = await response.Content.ReadFromJsonAsync<Sentence>();
            return sentence;
        }

        #endregion
    }
}
