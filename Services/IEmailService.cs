namespace TalentAI.Services;

public interface IEmailService
{
    Task SendHRCredentialsAsync(string toEmail, string temporaryPassword);
}
