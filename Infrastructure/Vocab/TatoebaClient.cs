using Infrastructure.Vocab.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Vocab
{
    public class TatoebaApiClient : IDisposable
    {
        private readonly HttpClient _http;

        public TatoebaApiClient(string baseUrl = "https://api.dev.tatoeba.org/unstable")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public void Dispose()
        {
            _http?.Dispose();
        }

        /// <summary>
        /// Obtém a lista de servidores da API.
        /// </summary>
        public async Task<List<ServerInfo>> GetServersAsync()
        {
            var servers = new List<ServerInfo>();
            servers.Add(new ServerInfo() { Name = "Dev", Url = "https://api.dev.tatoeba.org" });
            return servers;
        }

        /// <summary>
        /// Busca sentenças com filtros opcionais.
        /// </summary>
        /// <param name="language">Código ISO da língua (ex: "eng", "por").</param>
        /// <param name="query">Texto a buscar dentro da sentença.</param>
        /// <param name="page">Página (opcional).</param>
        /// <param name="pageSize">Itens por página (opcional).</param>
        public async Task<SentenceSearchResult> SearchSentencesAsync(string language = null, string query = null, bool exactSearch = true)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(language)) queryParams.Add($"lang={Uri.EscapeDataString(language)}");
            if (exactSearch) query = $"={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrEmpty(query)) queryParams.Add($"q={query}");
            queryParams.Add($"sort=relevance");

            var url = "unstable/sentences";
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

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
    }
}
