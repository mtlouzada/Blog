using Blog.Controllers;
using Blog.Data;
using Blog.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a aplicação inteira em memória (pipeline, filtros, autenticação, EF Core)
/// apontando para um SQL Server de verdade.
///
/// Só duas fronteiras são substituídas: o SMTP, porque não é responsabilidade
/// nossa, e o diretório de arquivos estáticos, para não sujar o repositório.
/// Todo o resto é o código que vai para produção.
/// </summary>
public sealed class BlogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 32+ caracteres: HMAC-SHA256 exige chave de no mínimo 256 bits.
    private const string TestJwtKey = "chave-de-teste-apenas-para-a-suite-de-integracao-0123456789";

    private readonly SqlServerTestDatabase _database = new();
    private readonly string _webRootPath =
        Path.Combine(Path.GetTempPath(), $"blog-tests-wwwroot-{Guid.NewGuid():N}");

    public FakeEmailService Emails { get; } = new();

    public string WebRootPath => _webRootPath;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(_webRootPath, "images"));

        // A aplicação carrega configuração para estado estático (Configuration.Load)
        // durante os top-level statements do Program, antes de qualquer ponto de
        // extensão do WebApplicationFactory rodar. Variável de ambiente é a única
        // costura que chega a tempo — ver docs/integration-tests.md.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ASPNETCORE_WEBROOT", _webRootPath);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _database.ConnectionString);
        Environment.SetEnvironmentVariable("JwtKey", TestJwtKey);
        Environment.SetEnvironmentVariable("ApiKeyName", "api_key");
        Environment.SetEnvironmentVariable("ApiKey", "chave-de-api-de-teste");
        Environment.SetEnvironmentVariable("Env", "test");

        // Tocar em Services constrói o host — e, com ele, o DbContext já apontando
        // para o banco descartável.
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BlogDataContext>();
            await context.Database.MigrateAsync();
        }

        await _database.InitializeRespawnerAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    /// <summary>
    /// Devolve a aplicação ao estado inicial antes de cada teste. Estado compartilhado
    /// não é só o banco: o IMemoryCache é singleton e sobrevive entre os testes, então
    /// precisa ser invalidado aqui também.
    /// </summary>
    public async Task ResetAsync()
    {
        await _database.ResetAsync();
        Services.GetRequiredService<IMemoryCache>().Remove(CategoryController.CategoriesCacheKey);
        Emails.Clear();
    }

    /// <summary>Abre um escopo de DI para inspecionar o banco direto, sem passar pela API.</summary>
    public async Task ExecuteDbAsync(Func<BlogDataContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDataContext>();
        await action(context);
    }

    public async Task<TResult> QueryDbAsync<TResult>(Func<BlogDataContext, Task<TResult>> query)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDataContext>();
        return await query(context);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();

        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, recursive: true);
    }
}

[CollectionDefinition(ApiCollection.Name)]
public sealed class ApiCollection : ICollectionFixture<BlogApiFactory>
{
    public const string Name = "API do Blog";
}
