namespace ProjectDefense.Web.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";

    public string FromAddress { get; set; } = "no-reply@projectdefense.local";

    public string FromName { get; set; } = "ProjectDefense";

    public SmtpSettings Smtp { get; set; } = new();

    public SendGridSettings SendGrid { get; set; } = new();

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 1025;

        public bool EnableSsl { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }
    }

    public sealed class SendGridSettings
    {
        public string? ApiKey { get; set; }
    }
}
