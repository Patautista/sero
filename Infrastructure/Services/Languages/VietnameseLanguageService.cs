using Catalyst;
using Infrastructure.Interfaces;
using Infrastructure.Lookup;
using Infrastructure.Services;
using Infrastructure.Vocab;
using Mosaik.Core;

namespace Infrastructure.Services.Languages;

public class VietnameseLanguageService : ILanguageService
{
    private const string XPF_RULES_URL = "https://cohenpr-xpf.github.io/XPF/conv_resources/rules/vi.rules";
    private ITranscriptionProvider? _xpfProvider;

    public string LanguageCode => AvailableCodes.Vietnamese;

    public bool HasConjugationTable => false;

    public Language GetCatalystLanguage() => Language.Vietnamese;

    public Lingua.Language GetLinguaLanguage() => Lingua.Language.Vietnamese;

    public void RegisterLanguageModel()
    {
        Catalyst.Models.Vietnamese.Register();
    }

    public string GetDefaultRssFeedUrl() => "https://vnexpress.net/rss/tin-moi-nhat.rss";

    public IEnumerable<IDefinitionProvider> GetDefinitionProviders()
    {
        IDefinitionProvider[] providers =
        [
            new CambridgeClient(new CambridgeConfig { LanguagePair = "english-vietnamese" })
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
        // Lazy-initialize the XPF provider
        _xpfProvider ??= new XpfTranscriptor(
            new Uri(XPF_RULES_URL),
            "Vietnamese XPF",
            LanguageCode
        );

        ITranscriptionProvider[] providers =
        [
            _xpfProvider
        ];
        return providers;
    }
}