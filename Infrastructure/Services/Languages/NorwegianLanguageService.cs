using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class NorwegianLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Norwegian;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Norwegian;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Nynorsk;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.Norwegian.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://www.nrk.no/toppsaker.rss";
}