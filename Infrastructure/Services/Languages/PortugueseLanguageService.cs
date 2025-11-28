using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class PortugueseLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Portuguese;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Portuguese;

    public string GetDefaultRssFeedUrl()
    {
        throw new NotImplementedException();
    }

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Portuguese;

    public void RegisterLanguageModel()
    {
        //Catalyst.Models.Portuguese.Register();
        throw new NotImplementedException("Portuguese model registration is not implemented yet.");
    }
    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        IDefinitionProvider[] providers =
        [
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english-portuguese" }),
            new DictCcClient(new DictCcConfig { LanguagePair = "enpt" })
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
        // Portuguese is not supported by toIPA
        return Array.Empty<ITranscriptionProvider>();
    }
}