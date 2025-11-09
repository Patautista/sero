using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Vocab.Models
{
    // Modelo de “server”
    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        // Adicione outros campos conforme retornado pela API
    }

    // Modelo de “sentence”
    public class Sentence
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        // Adicione outros campos conforme retornado pela API
    }

    // Lista de sentences (com paginação, se aplicável)
    public class SentenceSearchResult
    {
        [JsonPropertyName("data")]
        public List<Sentence> Data { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
    }
}
