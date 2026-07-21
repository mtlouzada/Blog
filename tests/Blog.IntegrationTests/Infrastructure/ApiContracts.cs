using System.Net.Http.Json;
using System.Text.Json;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Espelhos do contrato HTTP da API.
///
/// Os testes não reaproveitam os ViewModels do projeto de produção de propósito:
/// se alguém renomear uma propriedade, o teste tem que quebrar. Reaproveitando o
/// tipo, a quebra de contrato viajaria silenciosamente para os clientes da API.
/// </summary>
public sealed record ApiResult<T>(T? Data, List<string>? Errors)
{
    public IReadOnlyList<string> ErrorMessages => Errors ?? new List<string>();
}

public sealed record CategoryPayload(int Id, string Name, string Slug);

public sealed record CreatedAccountPayload(string User, string Password);

public sealed record PostSummaryPayload(
    int Id,
    string Title,
    string Slug,
    DateTime LastUpdateDate,
    string Category,
    string Author);

public sealed record PostListPayload(
    int Total,
    int Page,
    int PageSize,
    List<PostSummaryPayload> Posts);

public sealed record PostDetailPayload(
    int Id,
    string Title,
    string Slug,
    CategoryPayload Category,
    AuthorPayload Author);

public sealed record AuthorPayload(int Id, string Name, string Email);

public static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ApiResult<T>> ReadResultAsync<T>(this HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(Options);

        return result
               ?? throw new InvalidOperationException(
                   $"Resposta vazia de {response.RequestMessage?.RequestUri} ({(int)response.StatusCode}).");
    }

    /// <summary>Corpo cru — útil nas mensagens de falha quando a asserção quebra.</summary>
    public static Task<string> ReadRawAsync(this HttpResponseMessage response)
        => response.Content.ReadAsStringAsync();
}
