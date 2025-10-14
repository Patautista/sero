using DeepL;

namespace SupportServer
{
    public static class DeepLHelper
    {
        public static string NormalizeSourceLang(string code)
        {
            if (code.Equals("en", StringComparison.OrdinalIgnoreCase) || code.Equals("en-us", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.English;
            }
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) || code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Portuguese;
            }
            if (code.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Italian;
            }
            if (code.Equals("no", StringComparison.OrdinalIgnoreCase) || code.Equals("nb", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Norwegian;
            }
            if (code.Equals("vi", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException();
            }
            if (code.Equals("de", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.German;
            }
            if (code.Equals("zh", StringComparison.OrdinalIgnoreCase) || code.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || code.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Chinese;
            }
            return LanguageCode.English;
        }
        public static string NormalizeTargetLang(string code)
        {
            if (code.Equals("en", StringComparison.OrdinalIgnoreCase) || code.Equals("en-us", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.EnglishAmerican;
            }
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) || code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.PortugueseBrazilian;
            }
            if (code.Equals("it", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Italian;
            }
            if (code.Equals("no", StringComparison.OrdinalIgnoreCase) || code.Equals("nb", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Norwegian;
            }
            if (code.Equals("vi", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException();
            }
            if (code.Equals("de", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.German;
            }
            if (code.Equals("zh", StringComparison.OrdinalIgnoreCase) || code.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || code.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageCode.Chinese;
            }
            return LanguageCode.EnglishAmerican;
        }
    }
}
