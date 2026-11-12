using System.Net;
using System.Text.RegularExpressions;

namespace ProjectDefense.Web.Email;

public static partial class EmailTemplate
{
    public static string Wrap(string subject, string htmlBody) =>
        $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(subject)}</title></head>
        <body style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2933">
        <h2 style="margin:0 0 16px">ProjectDefense</h2>
        <div>{htmlBody}</div>
        </body>
        </html>
        """;

    public static string ToPlainText(string html)
    {
        var withLinks = AnchorRegex().Replace(html, match => match.Groups["text"].Value + " (" + match.Groups["href"].Value + ")");
        return WebUtility.HtmlDecode(TagRegex().Replace(withLinks, string.Empty));
    }

    [GeneratedRegex("<a[^>]*href=['\"](?<href>[^'\"]+)['\"][^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
