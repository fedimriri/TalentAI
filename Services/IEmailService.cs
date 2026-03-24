namespace TalentAI.Services;

public interface IEmailService
{
    Task SendHRCredentialsAsync(string toEmail, string temporaryPassword);
    Task SendCandidateStatusEmailAsync(string toEmail, string status, string jobTitle);
}
