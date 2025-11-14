using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

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
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english-italian" }),
            new DictCcClient(new DictCcConfig { LanguagePair = "enit" })
        ];
        return providers;
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        IExampleProvider[] providers =
        [
            new TatoebaApiClient(LanguageCode)
        ];
        return providers;
    }
}