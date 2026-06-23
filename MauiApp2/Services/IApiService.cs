namespace MauiApp2.Services
{
    public interface IApiService
    {
        Task<LexicalAnalysisResult> AnalyzeLexicalAsync(string text, string language);
        Task<MistakeAnalysisResult> DetectMistakesAsync(string text, string language);
        Task<string> GenerateTextAsync(string prompt);
        Task<byte[]> GetTTSAsync(string text, string language);
        Task<string> TranslateAsync(string text, string sourceLang, string targetLang);
    }
}