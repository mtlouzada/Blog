namespace Blog.Services;

public interface IEmailService
{
    bool Send(
        string toName,
        string toEmail,
        string subject,
        string body,
        string fromName = "Equipe do Blog",
        string fromEmail = "blogEmail@gmail.com");
}
