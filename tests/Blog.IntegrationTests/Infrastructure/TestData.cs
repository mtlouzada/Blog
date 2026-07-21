using Blog.Models;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Construtores de cenário. Cada teste declara só o que importa para ele;
/// o resto vem de um padrão válido, para que a leitura do teste não se perca
/// em campos irrelevantes.
/// </summary>
public static class TestData
{
    public static Category Category(string name = "Backend", string? slug = null) => new()
    {
        Name = name,
        Slug = slug ?? name.ToLowerInvariant()
    };

    public static Post Post(
        Category category,
        User author,
        string title = "Testes de integração na prática",
        string? slug = null,
        DateTime? lastUpdateDate = null) => new()
    {
        Title = title,
        Summary = "Resumo do artigo",
        Body = "Conteúdo do artigo",
        Slug = slug ?? Guid.NewGuid().ToString("N"),
        Category = category,
        Author = author,
        CreateDate = lastUpdateDate ?? new DateTime(2026, 1, 1),
        // A coluna é SMALLDATETIME (precisão de minuto): datas de cenário precisam
        // ser distantes o suficiente para que a ordenação seja determinística.
        LastUpdateDate = lastUpdateDate ?? new DateTime(2026, 1, 1)
    };
}
