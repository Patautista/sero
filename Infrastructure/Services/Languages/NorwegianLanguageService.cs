using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;
using System.Collections.Generic;
using System.Globalization;

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

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        IDefinitionProvider[] providers =
        [
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english-norwegian" }),
            new DictCcClient(new DictCcConfig { LanguagePair = "enno" })
        ];
        return providers;
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        // Get three-letter ISO code for Norwegian (nob for Bokmål)
        var threeLetterCode = new CultureInfo("nb").ThreeLetterISOLanguageName;

        IExampleProvider[] providers =
        [
            new TatoebaApiClient(new TatoebaConfig 
            { 
                TargetLanguageCode = threeLetterCode,
                DefaultExactSearch = false,
                DefaultPageSize = 10
            }),
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english-norwegian" })
        ];
        return providers;
    }
}