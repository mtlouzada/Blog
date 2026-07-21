using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blog.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;
using Xunit;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Base das classes de teste: um cliente HTTP pronto, banco limpo antes de cada
/// teste e atalhos para preparar cenário.
///
/// A limpeza acontece ANTES do teste, não depois: se um teste quebra, os dados
/// dele continuam no banco para inspeção.
/// </summary>
[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected const string DefaultPassword = "senha-de-teste-123";

    protected IntegrationTestBase(BlogApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected BlogApiFactory Factory { get; }

    protected HttpClient Client { get; }

    public Task InitializeAsync() => Factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Insere um grafo de entidades. O que já tem chave preenchida (um autor criado
    /// antes, por exemplo) entra como Unchanged: sem isso o EF tentaria inserir o
    /// mesmo usuário de novo, com identity explícito.
    /// </summary>
    protected Task SeedAsync(params object[] entities)
        => Factory.ExecuteDbAsync(async context =>
        {
            foreach (var entity in entities)
                context.ChangeTracker.TrackGraph(entity, node =>
                    node.Entry.State = node.Entry.IsKeySet
                        ? EntityState.Unchanged
                        : EntityState.Added);

            await context.SaveChangesAsync();
        });

    protected async Task<User> SeedUserAsync(
        string email = "autor@blog.dev",
        string password = DefaultPassword,
        string name = "Autora de Teste")
    {
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            Slug = email.Replace("@", "-").Replace(".", "-")
        };

        await SeedAsync(user);
        return user;
    }

    /// <summary>
    /// Faz login pelo endpoint real e devolve um cliente com o Bearer token.
    /// Gerar o JWT na mão seria mais rápido, mas deixaria de exercitar justamente
    /// o que costuma quebrar: emissão, assinatura e validação combinando entre si.
    /// </summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "autor@blog.dev",
        string password = DefaultPassword)
    {
        var response = await Client.PostAsJsonAsync("v1/accounts/login", new { email, password });

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "o login do cenário precisa passar: {0}",
            await response.ReadRawAsync());

        var token = (await response.ReadResultAsync<string>()).Data;
        token.Should().NotBeNullOrWhiteSpace();

        var authenticated = Factory.CreateClient();
        authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return authenticated;
    }
}
