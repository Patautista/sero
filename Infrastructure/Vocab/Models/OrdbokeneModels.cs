using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Vocab.Models
{
    public class ArticlesResponse
    {
        public Meta Meta { get; set; }
        public Dictionary<string, List<int>> Articles { get; set; }
    }

    public class Meta
    {
        public Dictionary<string, DictMeta> Dicts { get; set; }
    }

    public class DictMeta
    {
        public int Total { get; set; }
    }

    public class Article
    {
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        
        public string Submitted { get; set; }
        
        public List<string> Suggest { get; set; }
        
        public List<Lemma> Lemmas { get; set; }
        
        public ArticleBody Body { get; set; }
        
        [JsonPropertyName("to_index")]
        public List<string> ToIndex { get; set; }
        
        public string Author { get; set; }
        
        [JsonPropertyName("edit_state")]
        public string EditState { get; set; }
        
        public List<Referer> Referers { get; set; }
        
        public int Status { get; set; }
        
        public string Updated { get; set; }
    }

    public class Lemma
    {
        [JsonPropertyName("added_norm")]
        public bool AddedNorm { get; set; }
        
        [JsonPropertyName("final_lexeme")]
        public string FinalLexeme { get; set; }
        
        public int Hgno { get; set; }
        
        public int Id { get; set; }
        
        [JsonPropertyName("inflection_class")]
        public string InflectionClass { get; set; }
        
        [JsonPropertyName("lemma")]
        public string LemmaText { get; set; }
        
        [JsonPropertyName("split_inf")]
        public bool SplitInf { get; set; }
    }

    public class ArticleBody
    {
        public List<BodyElement> Etymology { get; set; }
        
        public List<BodyElement> Definitions { get; set; }
        
        public List<BodyElement> Pronunciation { get; set; }
    }

    public class BodyElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; }
        
        public string Content { get; set; }
        
        public List<BodyElement> Items { get; set; }
        
        public List<BodyElement> Elements { get; set; }
        
        // Use JsonElement to handle both string and int values
        public JsonElement? Id { get; set; }
        
        [JsonPropertyName("sub_definition")]
        public bool? SubDefinition { get; set; }
        
        public Quote Quote { get; set; }
        
        public Explanation Explanation { get; set; }
        
        public string Text { get; set; }
        
        [JsonPropertyName("article_id")]
        public int? ArticleId { get; set; }
        
        public Intro Intro { get; set; }
        
        // Use JsonElement to handle both string[] and object[] values
        public JsonElement? Lemmas { get; set; }
        
        public SubArticle Article { get; set; }
        
        public int? Status { get; set; }
        
        [JsonPropertyName("definition_id")]
        public int? DefinitionId { get; set; }
        
        [JsonPropertyName("definition_order")]
        public int? DefinitionOrder { get; set; }
        
        // Helper methods to get Id as different types
        public string GetIdAsString()
        {
            if (!Id.HasValue) return null;
            
            if (Id.Value.ValueKind == JsonValueKind.String)
                return Id.Value.GetString();
            
            if (Id.Value.ValueKind == JsonValueKind.Number)
                return Id.Value.GetInt32().ToString();
            
            return Id.Value.ToString();
        }
        
        public int? GetIdAsInt()
        {
            if (!Id.HasValue) return null;
            
            if (Id.Value.ValueKind == JsonValueKind.Number)
                return Id.Value.GetInt32();
            
            if (Id.Value.ValueKind == JsonValueKind.String)
            {
                var str = Id.Value.GetString();
                if (int.TryParse(str, out var result))
                    return result;
            }
            
            return null;
        }
        
        // Helper method to extract lemma strings
        public List<string> GetLemmasAsStrings()
        {
            if (!Lemmas.HasValue) return new List<string>();
            
            var result = new List<string>();
            
            if (Lemmas.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in Lemmas.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        // Simple string array: ["på huset"]
                        result.Add(item.GetString());
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        // Object array with "lemma" property
                        if (item.TryGetProperty("lemma", out var lemmaProperty))
                        {
                            result.Add(lemmaProperty.GetString());
                        }
                    }
                }
            }
            
            return result;
        }
    }

    public class Quote
    {
        public string Content { get; set; }
        
        public List<BodyElement> Items { get; set; }
    }

    public class Explanation
    {
        public string Content { get; set; }
        
        public List<BodyElement> Items { get; set; }
    }

    public class Intro
    {
        public string Content { get; set; }
        
        public List<BodyElement> Items { get; set; }
    }

    public class Referer
    {
        [JsonPropertyName("article_id")]
        public int? ArticleId { get; set; }
        
        [JsonPropertyName("art_id")]
        public int? ArtId { get; set; }
        
        public int Hgno { get; set; }
        
        public string Lemma { get; set; }
        
        [JsonPropertyName("word_form")]
        public string WordForm { get; set; }
    }

    public class SubArticle
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; }
        
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        
        public List<SubArticleLemma> Lemmas { get; set; }
        
        public ArticleBody Body { get; set; }
        
        [JsonPropertyName("article_type")]
        public string ArticleType { get; set; }
        
        public string Author { get; set; }
        
        [JsonPropertyName("dict_id")]
        public string DictId { get; set; }
        
        public bool Frontpage { get; set; }
        
        [JsonPropertyName("latest_status")]
        public int LatestStatus { get; set; }
        
        public string Owner { get; set; }
        
        public Dictionary<string, object> Properties { get; set; }
        
        [JsonPropertyName("referenced_by")]
        public List<Referer> ReferencedBy { get; set; }
        
        public string Updated { get; set; }
        
        public int Version { get; set; }
        
        [JsonPropertyName("word_class")]
        public string WordClass { get; set; }
    }

    public class SubArticleLemma
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; }
        
        public int Hgno { get; set; }
        
        public int Id { get; set; }
        
        public string Lemma { get; set; }
    }

    public class Definition
    {
        public List<Content> Content { get; set; }
    }

    public class Content
    {
        public string TextContent { get; set; }
    }
}
