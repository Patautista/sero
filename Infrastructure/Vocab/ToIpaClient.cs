using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Business.Interfaces;

namespace Infrastructure.Vocab
{
    public class ToIpaClient : ITranscriptionProvider
    {
        private string _langCode = "en-US";
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://toipa.org/transcribe";

        private static readonly HashSet<string> SupportedLanguages = new()
        {
            "es-ES",  // Spanish
            "vi-C",   // Vietnamese
            "nb-NO",  // Norwegian
            "it-IT",  // Italian
            "zh-CN",  // Chinese
            "de-DE",  // German
            "en-US",  // English (US)
            "en-GB",  // English (GB)
            "en-AU"   // English (AU)
        };

        public string ProviderName => "toIPA";
        public string LanguageCode => _langCode;
        public string TranscriptionType => "IPA";

        public ToIpaClient(string langCode) {
            _langCode = langCode;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public bool SupportsLanguage(string languageCode)
        {
            return SupportedLanguages.Contains(languageCode);
        }

        public async Task<string?> GetTranscriptionAsync(string word)
        {
            return await GetTranscription(word);
        }

        public async Task<string?> GetTranscriptionAsync(IEnumerable<string> words)
        {
            if (words == null || !words.Any())
                return null;

            // Join the words with spaces and get transcription for the combined text
            var combinedText = string.Join(" ", words.Where(w => !string.IsNullOrWhiteSpace(w)));
            return await GetTranscription(combinedText);
        }

        public async Task<string?> GetTranscription(string token) {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                
                // Build URL: https://toipa.org/transcribe/en-AU/hello
                string url = $"{BaseUrl}/{_langCode}/{Uri.EscapeDataString(token.ToLower())}";

                // Fetch the page
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                string html = await response.Content.ReadAsStringAsync();

                // Parse HTML
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Find the pronunciation div with class "pronunciation"
                var pronunciationNode = htmlDoc.DocumentNode
                    .SelectSingleNode("//div[@class='paragraph flex flex-wrap gap-2']");

                // Remove hidden elements
                var hiddenNodes = pronunciationNode?.SelectNodes(".//*[@hidden]");
                if (hiddenNodes != null)
                {
                    foreach (var node in hiddenNodes)
                    {
                        node.Remove();
                    }
                }

                if (pronunciationNode != null)
                {
                    return pronunciationNode.InnerText.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error fetching IPA transcription: {ex.Message}");
                return null;
            }
        }
    }
}
