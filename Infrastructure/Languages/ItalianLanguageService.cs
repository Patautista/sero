using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Languages.Interface;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Languages;

public class ItalianLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Italian;

    public bool HasConjugationTable => true;

    public Language GetCatalystLanguage() => Language.Italian;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Italian;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.Italian.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://www.ansa.it/sito/notizie/mondo/mondo_rss.xml";

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        IDefinitionProvider[] providers =
        [
            new DictCcClient(new DictCcConfig { LanguagePair = "enit" }),
            new WiktionaryClient(new WiktionaryOptions { TargetLanguage = new System.Globalization.CultureInfo("it") }),
        ];
        return providers;
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        IExampleProvider[] providers =
        [
            new TatoebaApiClient(new TatoebaConfig { TargetLanguageCode = LanguageCode })
        ];
        return providers;
    }

    public IEnumerable<ITranscriptionProvider> GetTranscriptionProviders()
    {
        ITranscriptionProvider[] providers =
        [
            new ToIpaClient("it-IT")
        ];
        return providers;
    }
}