using Business.Interfaces;
using Catalyst;
using Infrastructure.Languages.Interface;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Languages;

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

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        IExampleProvider[] providers =
         [
            new TatoebaApiClient(new TatoebaConfig { TargetLanguageCode = LanguageCode }), 
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english" })
         ];
        return providers;
    }

    public IEnumerable<ITranscriptionProvider> GetTranscriptionProviders()
    {
        ITranscriptionProvider[] providers =
        [
            new ToIpaClient("en-US")
        ];
        return providers;
    }
}