using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class ChineseLanguageService : ILanguageService
{
    public string LanguageCode => AvailableCodes.Chinese;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Chinese;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Chinese;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.Chinese.Register();
    }
}