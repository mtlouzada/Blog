using System.Net;
using Blog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Blog.IntegrationTests;

public class PostEndpointsTests : IntegrationTestBase
{
    public PostEndpointsTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Listagem_deve_devolver_a_pagina_pedida_da_mais_recente_para_a_mais_antiga()
    {
        var autor = await SeedUserAsync();
        var categoria = TestData.Category("Backend", "backend");

        await SeedAsync(
            TestData.Post(categoria, autor, "Post de janeiro", "post-01", new DateTime(2026, 1, 10)),
            TestData.Post(categoria, autor, "Post de fevereiro", "post-02", new DateTime(2026, 2, 10)),
            TestData.Post(categoria, autor, "Post de março", "post-03", new DateTime(2026, 3, 10)),
            TestData.Post(categoria, autor, "Post de abril", "post-04", new DateTime(2026, 4, 10)));

        var response = await Client.GetAsync("v1/posts?page=0&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var pagina = (await response.ReadResultAsync<PostListPayload>()).Data!;
        pagina.Total.Should().Be(4);
        pagina.Posts.Should().HaveCount(2);

        // A ordenação tem que valer sobre a coleção inteira, não sobre a página:
        // a primeira página precisa trazer os dois posts mais recentes.
        pagina.Posts.Select(x => x.Title)
            .Should().ContainInOrder("Post de abril", "Post de março");
    }

    [Fact]
    public async Task Listagem_deve_devolver_o_autor_e_a_categoria_formatados()
    {
        var autor = await SeedUserAsync("ana@blog.dev", name: "Ana");
        var categoria = TestData.Category("Carreira", "carreira");
        await SeedAsync(TestData.Post(categoria, autor, "Como estudar", "como-estudar"));

        var response = await Client.GetAsync("v1/posts");

        var post = (await response.ReadResultAsync<PostListPayload>()).Data!.Posts.Single();
        post.Category.Should().Be("Carreira");
        post.Author.Should().Be("Ana (ana@blog.dev)");
    }

    [Fact]
    public async Task Listagem_por_categoria_deve_filtrar_e_totalizar_apenas_aquela_categoria()
    {
        var autor = await SeedUserAsync();
        var backend = TestData.Category("Backend", "backend");
        var carreira = TestData.Category("Carreira", "carreira");

        await SeedAsync(
            TestData.Post(backend, autor, "EF Core na prática", "ef-core"),
            TestData.Post(backend, autor, "Testes de integração", "testes-integracao"),
            TestData.Post(carreira, autor, "Primeiro emprego", "primeiro-emprego"),
            TestData.Post(carreira, autor, "Entrevistas técnicas", "entrevistas"));

        var response = await Client.GetAsync("v1/posts/category/backend");

        var pagina = (await response.ReadResultAsync<PostListPayload>()).Data!;
        pagina.Posts.Should().HaveCount(2);
        pagina.Posts.Should().OnlyContain(x => x.Category == "Backend");

        // O total alimenta a paginação do cliente: contar todos os posts do blog
        // aqui faria o front paginar sobre um número que não existe.
        pagina.Total.Should().Be(2);
    }

    [Fact]
    public async Task Listagem_de_categoria_sem_posts_deve_devolver_lista_vazia()
    {
        var response = await Client.GetAsync("v1/posts/category/inexistente");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadResultAsync<PostListPayload>()).Data!.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task Detalhe_deve_trazer_o_post_com_autor_e_categoria()
    {
        var autor = await SeedUserAsync("ana@blog.dev", name: "Ana");
        var categoria = TestData.Category("Backend", "backend");
        var post = TestData.Post(categoria, autor, "EF Core na prática", "ef-core");
        await SeedAsync(post);

        var response = await Client.GetAsync($"v1/posts/{post.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var detalhe = (await response.ReadResultAsync<PostDetailPayload>()).Data!;
        detalhe.Title.Should().Be("EF Core na prática");
        detalhe.Category.Name.Should().Be("Backend");
        detalhe.Author.Email.Should().Be("ana@blog.dev");
    }

    [Fact]
    public async Task Detalhe_nao_pode_expor_o_hash_de_senha_do_autor()
    {
        var autor = await SeedUserAsync("ana@blog.dev");
        var categoria = TestData.Category("Backend", "backend");
        var post = TestData.Post(categoria, autor, slug: "ef-core");
        await SeedAsync(post);

        var response = await Client.GetAsync($"v1/posts/{post.Id}");

        var json = await response.ReadRawAsync();
        json.Should().NotContain("passwordHash", "a entidade User vaza para o contrato HTTP quando é serializada direto");
        json.Should().NotContain(autor.PasswordHash);
    }

    [Fact]
    public async Task Detalhe_de_post_inexistente_deve_devolver_404()
    {
        var response = await Client.GetAsync("v1/posts/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadResultAsync<PostDetailPayload>())
            .ErrorMessages.Should().Contain("Conteúdo não encontrado");
    }
}
