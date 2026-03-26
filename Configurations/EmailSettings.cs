namespace TalentAI.Configurations;

public class EmailSettings
{
    // ========================
    // SendGrid API (PRODUCTION — Railway)
    // ========================
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "TalentAI";

    // ========================
    // SMTP / MailKit (LOCAL development only)
    // Uncomment and use these if switching back to Gmail SMTP locally.
    // ========================
    // public string Host { get; set; } = string.Empty;
    // public int Port { get; set; }
    // public string Username { get; set; } = string.Empty;
    // public string Password { get; set; } = string.Empty;
}
