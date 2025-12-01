using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Languages.Interface;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Languages;

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

    public string GetDefaultRssFeedUrl() => "http://news.baidu.com/n?cmd=4&class=civilnews&tn=rss";

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IExampleProvider> GetExampleProviders()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ITranscriptionProvider> GetTranscriptionProviders()
    {
        ITranscriptionProvider[] providers =
        [
            new ToIpaClient("zh-CN")
        ];
        return providers;
    }
}