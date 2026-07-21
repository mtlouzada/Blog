using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Blog.IntegrationTests;

/// <summary>
/// Autenticação é o tipo de coisa que teste unitário sempre acha que está certa:
/// o pipeline real (middleware, esquema Bearer, validação da assinatura) só roda
/// quando a aplicação inteira sobe.
/// </summary>
public class AuthorizationTests : IntegrationTestBase
{
    private const string ImagemBase64 =
        "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=";

    public AuthorizationTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Endpoint_protegido_sem_token_deve_devolver_401()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/accounts/upload-image",
            new { base64Image = ImagemBase64 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoint_protegido_com_token_forjado_deve_devolver_401()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJmYWxzbyJ9.assinatura-invalida");

        var response = await client.PostAsJsonAsync(
            "v1/accounts/upload-image",
            new { base64Image = ImagemBase64 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_de_imagem_autenticado_deve_gravar_o_arquivo_e_atualizar_o_usuario()
    {
        await SeedUserAsync("ana@blog.dev");
        var client = await CreateAuthenticatedClientAsync("ana@blog.dev");

        var response = await client.PostAsJsonAsync(
            "v1/accounts/upload-image",
            new { base64Image = ImagemBase64 });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var usuario = await Factory.QueryDbAsync(db =>
            db.Users.AsNoTracking().SingleAsync(x => x.Email == "ana@blog.dev"));

        usuario.Image.Should().NotBeNullOrWhiteSpace();
        usuario.Image.Should().NotContain("localhost:0000", "a URL da imagem precisa apontar para o host real");

        var arquivo = Path.Combine(Factory.WebRootPath, "images", Path.GetFileName(usuario.Image!));
        File.Exists(arquivo).Should().BeTrue(
            "o arquivo tem que ser gravado na raiz web da aplicação, e não no diretório de trabalho do processo");
    }

    [Fact]
    public async Task Fluxo_completo_de_cadastro_ate_endpoint_protegido()
    {
        var cadastro = await Client.PostAsJsonAsync(
            "v1/accounts",
            new { name = "Matheus", email = "matheus@blog.dev" });
        cadastro.StatusCode.Should().Be(HttpStatusCode.OK, await cadastro.ReadRawAsync());

        var senha = (await cadastro.ReadResultAsync<CreatedAccountPayload>()).Data!.Password;

        var login = await Client.PostAsJsonAsync(
            "v1/accounts/login",
            new { email = "matheus@blog.dev", password = senha });
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.ReadRawAsync());

        var token = (await login.ReadResultAsync<string>()).Data!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var upload = await client.PostAsJsonAsync(
            "v1/accounts/upload-image",
            new { base64Image = ImagemBase64 });

        upload.StatusCode.Should().Be(HttpStatusCode.OK, await upload.ReadRawAsync());
    }
}
