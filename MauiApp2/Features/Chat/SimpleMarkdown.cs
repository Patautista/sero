using System.Net;
using System.Text.RegularExpressions;

namespace MauiApp2.Features.Chat
{
    /// <summary>
    /// Converts a limited subset of Markdown (bold, italic) to safe HTML for display in Blazor via MarkupString.
    /// Input is HTML-encoded first to prevent XSS, then markdown patterns are applied.
    /// Supported: **bold** and *italic* (asterisk-based only).
    /// </summary>
    internal static class SimpleMarkdown
    {
        // Bold must be matched before italic so **text** isn't partially consumed by *text*
        private static readonly Regex _bold = new(
            @"\*\*(.+?)\*\*",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex _italic = new(
            @"\*(.+?)\*",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public static string ToHtml(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return markdown ?? string.Empty;

            var html = WebUtility.HtmlEncode(markdown);
            html = _bold.Replace(html, "<strong>$1</strong>");
            html = _italic.Replace(html, "<em>$1</em>");
            return html;
        }
    }
}
