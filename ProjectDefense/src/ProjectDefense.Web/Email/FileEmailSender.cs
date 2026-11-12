using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace ProjectDefense.Web.Email;

public sealed partial class FileEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileEmailSender> _logger;

    public FileEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment environment, ILogger<FileEmailSender> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var directory = Path.Combine(_environment.ContentRootPath, _options.File.Directory);
        Directory.CreateDirectory(directory);

        var safeRecipient = InvalidFileNameChars().Replace(email, "_");
        var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{safeRecipient}.html";
        var path = Path.Combine(directory, fileName);

        await File.WriteAllTextAsync(path, EmailTemplate.Wrap(subject, htmlMessage), System.Text.Encoding.UTF8);

        _logger.LogWarning("E-mail '{Subject}' to {Email} saved to {Path}", subject, email, path);
        foreach (Match link in LinkRegex().Matches(htmlMessage))
        {
            _logger.LogWarning("Link: {Link}", System.Net.WebUtility.HtmlDecode(link.Groups["href"].Value));
        }
    }

    [GeneratedRegex("[^A-Za-z0-9@._-]")]
    private static partial Regex InvalidFileNameChars();

    [GeneratedRegex("href=['\"](?<href>[^'\"]+)['\"]", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();
}
