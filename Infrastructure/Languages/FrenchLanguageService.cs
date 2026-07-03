using Catalyst;
using Infrastructure.Languages.Interface;
using Business.Lookup;
using Infrastructure.Services;
using Business.Vocab;
using Mosaik.Core;
using Business.Interfaces;
using Infrastructure.Vocab;

namespace Business.Languages;

public class FrenchLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.French;

    public bool HasConjugationTable => true;

    public Language GetCatalystLanguage() => Language.French;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.French;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.French.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://www.ansa.it/sito/notizie/mondo/mondo_rss.xml";

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        IDefinitionProvider[] providers =
        [
            new DictCcClient(new DictCcConfig { LanguagePair = "enfr" }),
            new WiktionaryClient(new WiktionaryOptions { TargetLanguage = new System.Globalization.CultureInfo("fr") }),
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
            new ToIpaClient("fr-FR")
        ];
        return providers;
    }
}