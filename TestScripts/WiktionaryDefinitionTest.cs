using Infrastructure.Lookup;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace TestScripts
{
    public static class WiktionaryDefinitionTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Wiktionary Definition Test ===\n");

            var options = new WiktionaryOptions
            {
                TargetLanguage = new System.Globalization.CultureInfo("vi"),
                EnableCaching = false
            };

            var client = new WiktionaryClient(options);

            // Test word from the example - "gått" has definitions in multiple languages
            var word = "trùng";
            Console.WriteLine($"Looking up word: {word}");
            Console.WriteLine($"Target Language: {options.TargetLanguage.EnglishName}\n");

            try
            {
                // Test GetDefinitionsAsync
                Console.WriteLine("--- Structured Definitions ---");
                var definitions = await client.GetDefinitionsAsync(word);
                Console.WriteLine($"Provider: {definitions.ProviderName}");
                Console.WriteLine($"Word: {definitions.Word}");
                Console.WriteLine($"Total entries: {definitions.Entries.Count}");
                Console.WriteLine($"(Note: Only {options.TargetLanguage.EnglishName} entries are included)\n");

                foreach (var entry in definitions.Entries)
                {
                    Console.WriteLine($"Part of Speech: {entry.PartOfSpeech ?? "N/A"}");
                    Console.WriteLine($"Headword: {entry.Headword}");
                    
                    foreach (var meaning in entry.Meanings)
                    {
                        Console.WriteLine($"  - Definition: {meaning.Definition}");
                        Console.WriteLine($"    Language: {meaning.DefinitionLanguage}");
                        
                        if (meaning.Examples.Any())
                        {
                            Console.WriteLine($"    Examples:");
                            foreach (var example in meaning.Examples)
                            {
                                Console.WriteLine($"      * {example}");
                            }
                        }
                    }
                    Console.WriteLine();
                }

                // Test GetDefinitionsHtmlAsync
                Console.WriteLine("\n--- HTML Definitions ---");
                var html = await client.GetDefinitionsHtmlAsync(word);
                Console.WriteLine(html);

                // Test GetExamplesAsync
                Console.WriteLine("\n--- Examples ---");
                var examples = await client.GetExamplesAsync(word);
                Console.WriteLine($"Total examples: {examples.Count}");
                foreach (var example in examples)
                {
                    Console.WriteLine($"  - {example.Sentence}");
                    Console.WriteLine($"    Source: {example.Source}, Context: {example.Context}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n=== Test Complete ===");
        }
    }
}
