using System.Net;
using System.Net.Http.Json;
using Blog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;
using Xunit;

namespace Blog.IntegrationTests;

public class AccountEndpointsTests : IntegrationTestBase
{
    public AccountEndpointsTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Registro_deve_criar_usuario_com_senha_em_hash_e_enviar_o_email_de_boas_vindas()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/accounts",
            new { name = "Matheus", email = "matheus@blog.dev" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var payload = (await response.ReadResultAsync<CreatedAccountPayload>()).Data!;
        payload.User.Should().Be("matheus@blog.dev");
        payload.Password.Should().NotBeNullOrWhiteSpace();

        var persisted = await Factory.QueryDbAsync(db =>
            db.Users.AsNoTracking().SingleAsync(x => x.Email == "matheus@blog.dev"));

        persisted.Slug.Should().Be("matheus-blog-dev");
        persisted.PasswordHash.Should().NotBe(payload.Password, "a senha não pode ser gravada em texto puro");
        PasswordHasher.Verify(persisted.PasswordHash, payload.Password)
            .Should().BeTrue("o hash gravado precisa conferir com a senha devolvida ao usuário");

        Factory.Emails.Sent.Should().ContainSingle()
            .Which.ToEmail.Should().Be("matheus@blog.dev");
    }

    [Fact]
    public async Task Registro_com_email_ja_cadastrado_deve_devolver_400_e_nao_duplicar_o_usuario()
    {
        var conta = new { name = "Matheus", email = "matheus@blog.dev" };
        (await Client.PostAsJsonAsync("v1/accounts", conta)).EnsureSuccessStatusCode();

        var response = await Client.PostAsJsonAsync("v1/accounts", conta);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadResultAsync<string>())
            .ErrorMessages.Should().Contain(x => x.Contains("já está cadastrado"));

        (await Factory.QueryDbAsync(db => db.Users.CountAsync())).Should().Be(1);
        Factory.Emails.Sent.Should().HaveCount(1, "o segundo cadastro falhou, então não houve o que notificar");
    }

    [Fact]
    public async Task Registro_com_email_invalido_deve_devolver_400_sem_enviar_email()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/accounts",
            new { name = "Matheus", email = "isto-nao-e-um-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadResultAsync<string>()).ErrorMessages.Should().Contain("O E-mail é inválido");

        Factory.Emails.Sent.Should().BeEmpty();
        (await Factory.QueryDbAsync(db => db.Users.CountAsync())).Should().Be(0);
    }

    [Fact]
    public async Task Login_com_credenciais_validas_deve_devolver_um_jwt()
    {
        await SeedUserAsync("matheus@blog.dev");

        var response = await Client.PostAsJsonAsync(
            "v1/accounts/login",
            new { email = "matheus@blog.dev", password = DefaultPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.ReadRawAsync());

        var token = (await response.ReadResultAsync<string>()).Data!;
        token.Split('.').Should().HaveCount(3, "um JWT tem header, payload e assinatura");
    }

    [Fact]
    public async Task Login_com_senha_errada_deve_devolver_401()
    {
        await SeedUserAsync("matheus@blog.dev");

        var response = await Client.PostAsJsonAsync(
            "v1/accounts/login",
            new { email = "matheus@blog.dev", password = "senha-errada" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.ReadResultAsync<string>())
            .ErrorMessages.Should().Contain("Usuário ou senha inválidos");
    }

    [Fact]
    public async Task Login_de_usuario_inexistente_deve_devolver_a_mesma_mensagem_generica()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/accounts/login",
            new { email = "ninguem@blog.dev", password = DefaultPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Mensagem idêntica à de senha errada: senão a API vira um oráculo que
        // confirma quais e-mails existem na base.
        (await response.ReadResultAsync<string>())
            .ErrorMessages.Should().Contain("Usuário ou senha inválidos");
    }

    [Fact]
    public async Task Login_sem_senha_deve_devolver_400_de_validacao()
    {
        var response = await Client.PostAsJsonAsync(
            "v1/accounts/login",
            new { email = "matheus@blog.dev" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadResultAsync<string>()).ErrorMessages.Should().Contain("Informe a senha");
    }
}
