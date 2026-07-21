using System.Net;
using System.Net.Http.Json;
using Blog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Blog.IntegrationTests;

public class CategoryEndpointsTests : IntegrationTestBase
{
    public CategoryEndpointsTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Post_deve_persistir_a_categoria_e_devolver_201_com_location()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/categories",
            new { name = "Backend", slug = "BACKEND" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.ReadRawAsync());

        var created = (await response.ReadResultAsync<CategoryPayload>()).Data!;
        created.Id.Should().BeGreaterThan(0);
        created.Slug.Should().Be("backend", "o endpoint normaliza o slug para minúsculas");

        response.Headers.Location!.ToString().Should().EndWith($"v1/categories/{created.Id}");

        // A resposta pode mentir: quem confirma a escrita é o banco.
        var persisted = await Factory.QueryDbAsync(db =>
            db.Categories.AsNoTracking().SingleAsync(x => x.Id == created.Id));

        persisted.Name.Should().Be("Backend");
        persisted.Slug.Should().Be("backend");
    }

    [Fact]
    public async Task Post_com_nome_invalido_deve_devolver_400_sem_gravar_nada()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/categories",
            new { name = "ab", slug = "ab" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadResultAsync<CategoryPayload>();
        result.ErrorMessages.Should().Contain("Este campo deve conter entre 3 e 40 caracteres");

        (await Factory.QueryDbAsync(db => db.Categories.CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task Post_com_slug_repetido_deve_devolver_409_e_manter_uma_unica_categoria()
    {
        await Client.PostAsJsonAsync("v1/categories", new { name = "Backend", slug = "backend" });

        var response = await Client.PostAsJsonAsync(
            "v1/categories",
            new { name = "Back-end", slug = "backend" });

        // O índice único vive no banco. Sem banco de verdade no teste, este caminho
        // de erro nunca seria exercitado.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict, await response.ReadRawAsync());

        (await Factory.QueryDbAsync(db => db.Categories.CountAsync())).Should().Be(1);
    }

    [Fact]
    public async Task Listagem_deve_refletir_categoria_criada_logo_em_seguida()
    {
        await Client.GetAsync("v1/categories"); // aquece o cache com a lista vazia

        var criada = await Client.PostAsJsonAsync(
            "v1/categories",
            new { name = "Carreira", slug = "carreira" });
        criada.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await Client.GetAsync("v1/categories");

        var categorias = (await response.ReadResultAsync<List<CategoryPayload>>()).Data!;
        categorias.Should().ContainSingle(x => x.Slug == "carreira",
            "escrever e ler em seguida é o fluxo mais comum da API; o cache não pode servir uma lista vencida");
    }

    [Fact]
    public async Task Get_por_id_deve_devolver_a_categoria()
    {
        var categoria = TestData.Category("Arquitetura", "arquitetura");
        await SeedAsync(categoria);

        var response = await Client.GetAsync($"v1/categories/{categoria.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadResultAsync<CategoryPayload>()).Data!.Name.Should().Be("Arquitetura");
    }

    [Fact]
    public async Task Get_por_id_inexistente_deve_devolver_404()
    {
        var response = await Client.GetAsync("v1/categories/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadResultAsync<CategoryPayload>())
            .ErrorMessages.Should().Contain("Conteúdo não encontrado");
    }

    [Fact]
    public async Task Put_deve_atualizar_a_categoria_persistida()
    {
        var categoria = TestData.Category("Backend", "backend");
        await SeedAsync(categoria);

        var response = await Client.PutAsJsonAsync(
            $"v1/categories/{categoria.Id}",
            new { name = "Backend .NET", slug = "backend-dotnet" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var persisted = await Factory.QueryDbAsync(db =>
            db.Categories.AsNoTracking().SingleAsync(x => x.Id == categoria.Id));

        persisted.Name.Should().Be("Backend .NET");
        persisted.Slug.Should().Be("backend-dotnet");
    }

    [Fact]
    public async Task Put_em_categoria_inexistente_deve_devolver_404()
    {
        var response = await Client.PutAsJsonAsync(
            "v1/categories/9999",
            new { name = "Qualquer", slug = "qualquer" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_deve_remover_a_categoria_do_banco()
    {
        var categoria = TestData.Category("Temporária", "temporaria");
        await SeedAsync(categoria);

        var response = await Client.DeleteAsync($"v1/categories/{categoria.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        (await Factory.QueryDbAsync(db => db.Categories.CountAsync())).Should().Be(0);
        (await Client.GetAsync($"v1/categories/{categoria.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
