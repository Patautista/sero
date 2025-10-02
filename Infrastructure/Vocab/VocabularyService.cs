using Business;
using Catalyst;
using DeepL.Model;
using Mosaik.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Language = Mosaik.Core.Language;

namespace Infrastructure.Services;

public class VocabularyService
{
    private readonly CultureInfo TargetLanguage;
    public VocabularyService(ISettingsService settingsService)
    {
        TargetLanguage = settingsService.StudyConfig.Value.SelectedLanguage.Target;
    }
    public VocabularyService(string targetLangCode)
    {
        TargetLanguage = new(targetLangCode);
    }
    public VocabularyService(CultureInfo targetLanguage)
    {
        TargetLanguage = targetLanguage;
    }
    // Universal POS tag mapping (UD v2) to common names
    private static readonly Dictionary<string, string> PosCommonNames = new()
    {
        { "ADJ", "Adjective" },
        { "ADP", "Adposition" },
        { "ADV", "Adverb" },
        { "AUX", "Auxiliary" },
        { "CCONJ", "Coordinating conjunction" },
        { "DET", "Determiner" },
        { "INTJ", "Interjection" },
        { "NOUN", "Noun" },
        { "NUM", "Numeral" },
        { "PART", "Particle" },
        { "PRON", "Pronoun" },
        { "PROPN", "Proper noun" },
        { "PUNCT", "Punctuation" },
        { "SCONJ", "Subordinating conjunction" },
        { "SYM", "Symbol" },
        { "VERB", "Verb" },
        { "X", "Other" }
    };

    public int GetVocabularyCount(IEnumerable<string> texts)
    {
        var combinedText = string.Join(" ", texts).ToLower();
        var words = combinedText.Split(new char[] { ' ', '.', ',', ';', ':', '-', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        var vocab = new HashSet<string>(words);
        return vocab.Count;
    }

    public async Task<Dictionary<string, int>> GetVocabByPartOfSpeechAsync(IEnumerable<string> texts)
    {
        var language = Language.English;
        if (TargetLanguage.TwoLetterISOLanguageName == AvailableCodes.Italian)
        {
            language = Language.Italian;
            Catalyst.Models.Italian.Register();
        }
        if (TargetLanguage.TwoLetterISOLanguageName == AvailableCodes.Norwegian)
        {
            language = Language.Norwegian;
            Catalyst.Models.Norwegian.Register();
        }
        var nlp = await Pipeline.ForAsync(language);

        var combinedText = string.Join(" ", texts);
        var doc = new Document(combinedText, language);
        nlp.ProcessSingle(doc);

        var posVocab = new Dictionary<string, HashSet<string>>();

        foreach (var sentence in doc)
        {
            foreach (var token in sentence)
            {
                var posTag = token.POS.ToString();
                var word = token.Value.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(word) || word.Length < 2) continue;

                // Map POS tag to common name, fallback to tag if not found
                var pos = PosCommonNames.TryGetValue(posTag, out var commonName) ? commonName : posTag;

                if (!posVocab.ContainsKey(pos))
                    posVocab[pos] = new HashSet<string>();
                posVocab[pos].Add(word);
            }
        }

        return posVocab.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }

    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
    }

    public async Task<List<Word>> GetVocabWordsWithTagAsync(IEnumerable<string> texts)
    {
        var language = Language.English;
        if (TargetLanguage.TwoLetterISOLanguageName == AvailableCodes.Italian)
        {
            language = Language.Italian;
            Catalyst.Models.Italian.Register();
        }
        if (TargetLanguage.TwoLetterISOLanguageName == AvailableCodes.Norwegian)
        {
            language = Language.Norwegian;
            Catalyst.Models.Norwegian.Register();
        }
        var nlp = await Pipeline.ForAsync(language);

        var combinedText = string.Join(" ", texts);
        var doc = new Document(combinedText, language);
        nlp.ProcessSingle(doc);

        var words = new List<Word>();

        var seen = new HashSet<(string, string)>();

        foreach (var sentence in doc)
        {
            foreach (var token in sentence)
            {
                var posTag = token.POS.ToString();
                var wordText = token.Value.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(wordText) || wordText.Length < 2) continue;

                var pos = PosCommonNames.TryGetValue(posTag, out var commonName) ? commonName : posTag;

                // Avoid duplicates (word + pos)
                if (seen.Add((wordText, pos)))
                {
                    words.Add(new Word
                    {
                        Text = wordText,
                        PartOfSpeech = pos
                    });
                }
            }
        }

        return words;
    }
}