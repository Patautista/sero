using Business.Interfaces;
using Infrastructure.Vocab.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Vocab
{
    public class OrdbokeneClient : IDefinitionProvider, IExampleProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _defaultDict;
        private readonly List<string> _dicts;

        #region IDefinitionProvider Properties
        public string ProviderName => "Ordbøkene";
        public string LanguagePair => "norwegian-norwegian"; // Monolingual Norwegian dictionary
        public bool SupportsStructuredOutput => true;
        #endregion

        #region IExampleProvider Properties
        string IExampleProvider.ProviderName => "Ordbøkene";
        public string LanguageCode => "nob"; // Norwegian Bokmål
        public bool SupportsAllLanguages => false;
        public bool SupportsSearch => true;
        public bool SupportsExactMatch => true;
        public bool SupportsTranslations => false; // Monolingual dictionary
        #endregion

        public OrdbokeneClient(string baseUrl = "https://ord.uib.no", string defaultDict = "bm")
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _defaultDict = defaultDict;
            _dicts = new List<string> { "bm" }; // only Bokmål. (Nynorsk: nn)
        }

        #region Original Methods

        /// <summary>
        /// Busca a lista de artigos para um determinado "w" (palavra), dicionários e scope.
        /// </summary>
        /// <param name="w">Palavra exata ou com wildcard</param>
        /// <param name="wc">Classe gramatical (opcional)</param>
        /// <param name="dicts">Lista de dicionários, por exemplo ["bm","nn"]</param>
        /// <param name="scope">Scope de busca, por exemplo "ef" ("e" = exact, "f" = freetext)</param>
        public async Task<ArticlesResponse> GetArticlesAsync(string w, string wc = null, IEnumerable<string> dicts = null, string scope = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(w)) query.Add($"w={Uri.EscapeDataString(w)}");
            if (!string.IsNullOrEmpty(wc)) query.Add($"wc={Uri.EscapeDataString(wc)}");
            if (dicts != null) query.Add($"dict={Uri.EscapeDataString(string.Join(',', dicts))}");
            if (!string.IsNullOrEmpty(scope)) query.Add($"scope={Uri.EscapeDataString(scope)}");

            var url = "/api/articles";
            if (query.Count > 0) url += "?" + string.Join("&", query);

            var resp = await _httpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();
            var articles = await JsonSerializer.DeserializeAsync<ArticlesResponse>(stream, _jsonOptions);
            return articles;
        }

        /// <summary>
        /// Busca o "artigo" JSON completo a partir do article_id e do dicionário.
        /// </summary>
        /// <param name="dict">Por exemplo "bm" ou "nn"</param>
        /// <param name="articleId">ID do artigo</param>
        public async Task<Article> GetArticleAsync(string dict, int articleId)
        {
            try
            {
                // Endpoint segundo documentação: /{dict}/article/{article_id}.json
                var url = $"/{dict}/article/{articleId}.json";
                var resp = await _httpClient.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var article = JsonSerializer.Deserialize<Article>(json, _jsonOptions);
                return article;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        #endregion

        #region IDefinitionProvider Implementation

        public async Task<DefinitionResult> GetDefinitionsAsync(string word)
        {
            var result = new DefinitionResult
            {
                Word = word,
                ProviderName = ProviderName,
                Entries = new List<DefinitionEntry>()
            };

            try
            {
                // Search for articles with exact match
                var articlesResponse = await GetArticlesAsync(word, dicts: _dicts, scope: "e");
                
                if (articlesResponse?.Articles == null)
                    return result;

                // Process articles from each dictionary
                foreach (var dictEntry in articlesResponse.Articles)
                {
                    var dict = dictEntry.Key;
                    var articleIds = dictEntry.Value;

                    foreach (var articleId in articleIds.Take(3)) // Limit to first 3 articles per dictionary
                    {
                        var article = await GetArticleAsync(dict, articleId);
                        if (article?.Body?.Definitions == null) continue;

                        // Get word class from lemmas
                        var wordClass = article.Lemmas?.FirstOrDefault()?.InflectionClass ?? "";

                        var entry = new DefinitionEntry
                        {
                            Headword = word,
                            PartOfSpeech = wordClass,
                            Meanings = new List<DefinitionMeaning>()
                        };

                        foreach (var definition in article.Body.Definitions.FirstOrDefault()?.Elements ?? new List<BodyElement>())
                        {
                            if (definition?.Elements == null) continue;

                            var defText = ExtractDefinitionText(definition);
                            var examples = ExtractExamples(definition);

                            if (!string.IsNullOrWhiteSpace(defText))
                            {
                                var meaning = new DefinitionMeaning
                                {
                                    Definition = defText,
                                    DefinitionLanguage = "nb", // Norwegian Bokmål
                                    Translation = "", // Monolingual dictionary
                                    Examples = examples
                                };

                                entry.Meanings.Add(meaning);
                            }
                        }

                        try
                        {
                            foreach (var definition in article.Body.Definitions ?? new List<BodyElement>())
                            {
                                if (definition?.Elements == null) continue;

                                var defText = ExtractDefinitionText(definition);
                                var examples = ExtractExamples(definition);

                                if (!string.IsNullOrWhiteSpace(defText))
                                {
                                    var meaning = new DefinitionMeaning
                                    {
                                        Definition = defText,
                                        DefinitionLanguage = "nb", // Norwegian Bokmål
                                        Translation = "", // Monolingual dictionary
                                        Examples = examples
                                    };

                                    entry.Meanings.Add(meaning);
                                }
                            }
                        }
                        finally{}

                        if (entry.Meanings.Any())
                        {
                            result.Entries.Add(entry);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("");
                // Return empty result on error
            }

            return result;
        }

        public async Task<string> GetDefinitionsHtmlAsync(string word)
        {
            var definitions = await GetDefinitionsAsync(word);
            var html = new StringBuilder();

            html.Append("<div class='ordbokene-definitions'>");
            html.Append($"<h3>{word}</h3>");

            foreach (var entry in definitions.Entries)
            {
                html.Append("<div class='entry'>");
                html.Append($"<div class='headword'><strong>{entry.Headword}</strong>");
                
                if (!string.IsNullOrEmpty(entry.PartOfSpeech))
                {
                    html.Append($" <em>({entry.PartOfSpeech})</em>");
                }
                
                html.Append("</div>");

                html.Append("<ol class='meanings'>");
                foreach (var meaning in entry.Meanings)
                {
                    html.Append("<li>");
                    html.Append($"<span class='definition'>{meaning.Definition}</span>");
                    
                    if (meaning.Examples.Any())
                    {
                        html.Append("<ul class='examples'>");
                        foreach (var example in meaning.Examples)
                        {
                            html.Append($"<li>{example}</li>");
                        }
                        html.Append("</ul>");
                    }
                    
                    html.Append("</li>");
                }
                html.Append("</ol>");
                html.Append("</div>");
            }

            html.Append("</div>");
            return html.ToString();
        }

        public async Task<List<Example>> GetExamplesAsync(string word)
        {
            var examples = new List<Example>();

            try
            {
                var definitions = await GetDefinitionsAsync(word);
                
                foreach (var entry in definitions.Entries)
                {
                    foreach (var meaning in entry.Meanings)
                    {
                        foreach (var exampleText in meaning.Examples)
                        {
                            examples.Add(new Example
                            {
                                Sentence = exampleText,
                                Source = ProviderName,
                                Context = $"{entry.Headword} ({entry.PartOfSpeech})"
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return empty list on error
            }

            return examples;
        }

        #endregion

        #region IExampleProvider Implementation

        public async Task<ExampleSearchResult> SearchExamplesAsync(
            string query, 
            string languageCode, 
            bool exactMatch = false, 
            int maxResults = 10)
        {
            var result = new ExampleSearchResult
            {
                Query = query,
                LanguageCode = languageCode,
                Page = 1,
                PageSize = maxResults,
                Examples = new List<ExampleSentence>()
            };

            try
            {
                // Use the scope parameter: "e" for exact, "f" for freetext
                var scope = exactMatch ? "e" : "ef";
                var articlesResponse = await GetArticlesAsync(query, dicts: _dicts, scope: scope);
                
                if (articlesResponse?.Articles == null)
                    return result;

                var exampleCount = 0;

                foreach (var dictEntry in articlesResponse.Articles)
                {
                    if (exampleCount >= maxResults) break;

                    var dict = dictEntry.Key;
                    var articleIds = dictEntry.Value;

                    foreach (var articleId in articleIds)
                    {
                        if (exampleCount >= maxResults) break;

                        var article = await GetArticleAsync(dict, articleId);
                        if (article?.Body?.Definitions == null) continue;

                        // Get word class from lemmas
                        var wordClass = article.Lemmas?.FirstOrDefault()?.InflectionClass ?? "";

                        foreach (var definition in article.Body.Definitions)
                        {
                            if (exampleCount >= maxResults) break;
                            if (definition?.Elements == null) continue;

                            var examples = ExtractExamples(definition);
                            foreach (var exampleText in examples)
                            {
                                if (exampleCount >= maxResults) break;

                                result.Examples.Add(new ExampleSentence
                                {
                                    Id = $"{dict}_{articleId}_{exampleCount}",
                                    Text = exampleText,
                                    Language = languageCode,
                                    Source = ProviderName,
                                    Context = wordClass,
                                    Metadata = new Dictionary<string, object>
                                    {
                                        { "dictionary", dict },
                                        { "articleId", articleId }
                                    }
                                });

                                exampleCount++;
                            }
                        }
                    }
                }

                result.TotalResults = exampleCount;
            }
            catch (Exception)
            {
                // Return empty result on error
            }

            return result;
        }

        public async Task<List<ExampleSentence>> GetExamplesForWordAsync(string word, string languageCode)
        {
            var searchResult = await SearchExamplesAsync(word, languageCode, exactMatch: true, maxResults: 20);
            return searchResult.Examples.ToList();
        }

        #endregion

        #region Helper Methods

        private string ExtractDefinitionText(List<Content> content)
        {
            if (content == null || !content.Any())
                return string.Empty;

            var texts = content
                .Where(c => !string.IsNullOrWhiteSpace(c.TextContent))
                .Select(c => c.TextContent.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t));

            return string.Join(" ", texts);
        }

        private string ExtractDefinitionText(BodyElement element)
        {
            if (element == null)
                return string.Empty;

            var texts = new List<string>();

            if (element.Content != null)
            {
                texts.Add(ReplacePlaceholders(element.Content, element.Items));
            }

            if (element.Text != null)
            {
                texts.Add(element.Text);
            }

            if (element.Elements != null)
            {
                foreach (var subElement in element.Elements)
                {
                    if (subElement.Type == "explanation" && subElement.Content != null)
                    {
                        texts.Add(ReplacePlaceholders(subElement.Content, subElement.Items));
                    }
                }
            }

            return string.Join(" ", texts.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        private List<string> ExtractExamples(List<Content> content)
        {
            var examples = new List<string>();
            
            if (content == null)
                return examples;

            // Look for content that appears to be examples
            // This is a simplified approach - adjust based on actual API response structure
            foreach (var item in content)
            {
                if (string.IsNullOrWhiteSpace(item.TextContent))
                    continue;

                var text = item.TextContent.Trim();
                
                // Simple heuristic: examples often contain full sentences with punctuation
                if (text.Length > 10 && (text.Contains('.') || text.Contains('!') || text.Contains('?')))
                {
                    examples.Add(text);
                }
            }

            return examples;
        }

        private List<string> ExtractExamples(BodyElement element)
        {
            var examples = new List<string>();

            if (element?.Elements == null)
                return examples;

            foreach (var subElement in element.Elements)
            {
                if (subElement.Type == "example" && subElement.Quote?.Content != null)
                {
                    var exampleText = ReplacePlaceholders(subElement.Quote.Content, subElement.Quote.Items);
                    if (!string.IsNullOrWhiteSpace(exampleText))
                    {
                        examples.Add(exampleText);
                    }
                }
                
                // Recursively check sub-elements
                if (subElement.Elements != null)
                {
                    examples.AddRange(ExtractExamples(subElement));
                }
            }

            return examples;
        }

        /// <summary>
        /// Replaces '$' placeholders in content with values from items array
        /// </summary>
        /// <param name="content">The content string that may contain '$' placeholders</param>
        /// <param name="items">The items array containing replacement values</param>
        /// <returns>Content string with placeholders replaced</returns>
        private string ReplacePlaceholders(string content, List<BodyElement> items)
        {
            if (string.IsNullOrEmpty(content) || !content.Contains('$'))
                return content;

            if (items == null || !items.Any())
                return content;

            var result = content;
            var placeholderIndex = 0;

            // Process each '$' in order
            while (result.Contains('$') && placeholderIndex < items.Count)
            {
                var item = items[placeholderIndex];
                var replacement = GetReplacementValue(item);

                if (!string.IsNullOrEmpty(replacement))
                {
                    // Replace the first occurrence of '$'
                    var index = result.IndexOf('$');
                    if (index >= 0)
                    {
                        result = result.Substring(0, index) + replacement + result.Substring(index + 1);
                    }
                }

                placeholderIndex++;
            }

            return result;
        }

        /// <summary>
        /// Extracts the replacement value from a BodyElement item
        /// </summary>
        /// <param name="item">The item to extract value from</param>
        /// <returns>The replacement text, or empty string if not found</returns>
        private string GetReplacementValue(BodyElement item)
        {
            if (item == null)
                return string.Empty;

            // For "usage" type, get the "text" property
            if (item.Type == "usage" && !string.IsNullOrEmpty(item.Text))
            {
                return item.Text;
            }

            // For "article_ref" type, get the first lemma
            if (item.Type == "article_ref")
            {
                var lemmas = item.GetLemmasAsStrings();
                if (lemmas.Any())
                {
                    return lemmas.First();
                }
            }

            // For "entity" type, get the id (like "norr.")
            if (item.Type == "entity")
            {
                var id = item.GetIdAsString();
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            return string.Empty;
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
