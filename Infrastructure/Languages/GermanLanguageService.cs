using Business.Interfaces;
using Business.Lookup;
using Catalyst;
using Infrastructure.Languages.Interface;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Languages;

public class GermanLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.German;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.German;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.German;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.German.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://www.dw.com/de/top-themen/s-100135";

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        return new IDefinitionProvider[]
        {
            new DictCcClient(new DictCcConfig { LanguagePair = "deen" }),
            new WiktionaryClient(new WiktionaryOptions { TargetLanguage = new System.Globalization.CultureInfo("de") }),
        };
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ITranscriptionProvider> GetTranscriptionProviders()
    {
        ITranscriptionProvider[] providers =
        [
            new ToIpaClient("de-DE")
        ];
        return providers;
    }
}