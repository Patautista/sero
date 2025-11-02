using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
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
}