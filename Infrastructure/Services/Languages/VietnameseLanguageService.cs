using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class VietnameseLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Vietnamese;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Vietnamese;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Vietnamese;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.Vietnamese.Register();
    }
}