using AspireApp1.ApiService;

class Program
{
    static async Task Main()
    {
        var config = new DictCcConfig
        {
            LanguagePair = "deen",  // German-English
            MaxResults = 5,
            EnableCaching = true
        };

        var client = new DictCcClient(config);

        var translations = await client.TranslateAsync("house");

        Console.WriteLine("Translations for 'house':");
        foreach (var t in translations)
        {
            Console.WriteLine(" - " + t);
        }
    }
}
