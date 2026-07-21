using System.Collections.Concurrent;
using Blog.Services;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Substitui o SMTP real na suíte. Teste de integração cobre a nossa aplicação
/// de ponta a ponta, não a disponibilidade do provedor de e-mail: a fronteira
/// externa vira um dublê observável, e o envio passa a ser algo que dá para afirmar.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    /// <summary>Simula uma indisponibilidade do provedor quando ligado.</summary>
    public bool ShouldFail { get; set; }

    public bool Send(
        string toName,
        string toEmail,
        string subject,
        string body,
        string fromName = "Equipe do Blog",
        string fromEmail = "blogEmail@gmail.com")
    {
        if (ShouldFail)
            return false;

        _sent.Enqueue(new SentEmail(toName, toEmail, subject, body, fromName, fromEmail));
        return true;
    }

    public void Clear()
    {
        _sent.Clear();
        ShouldFail = false;
    }

    public sealed record SentEmail(
        string ToName,
        string ToEmail,
        string Subject,
        string Body,
        string FromName,
        string FromEmail);
}
