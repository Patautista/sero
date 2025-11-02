using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

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
}