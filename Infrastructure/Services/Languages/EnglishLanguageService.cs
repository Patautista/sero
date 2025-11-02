using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class EnglishLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.English;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.English;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.English;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.English.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://rss.nytimes.com/services/xml/rss/nyt/World.xml";
}