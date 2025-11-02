using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class PortugueseLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Portuguese;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Portuguese;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Portuguese;

    public void RegisterLanguageModel()
    {
        //Catalyst.Models.Portuguese.Register();
        throw new NotImplementedException("Portuguese model registration is not implemented yet.");
    }
}