using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace Blog.IntegrationTests.Infrastructure;

/// <summary>
/// Banco de dados descartável para a suíte: cria um database exclusivo por execução,
/// aplica as migrations reais e devolve o estado ao ponto zero entre os testes.
///
/// O servidor pode ser trocado pela variável de ambiente BLOG_TESTS_SQLSERVER
/// (é assim que o CI aponta para o container do SQL Server). Sem ela, usa o
/// LocalDB da máquina do desenvolvedor.
/// </summary>
public sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private const string DefaultServerConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;";

    private readonly string _databaseName = $"BlogTests_{Guid.NewGuid():N}";
    private readonly string _serverConnectionString;
    private Respawner? _respawner;

    public SqlServerTestDatabase()
    {
        _serverConnectionString =
            Environment.GetEnvironmentVariable("BLOG_TESTS_SQLSERVER")
            ?? DefaultServerConnectionString;

        ConnectionString = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = _databaseName
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    /// <summary>
    /// Prepara o Respawner depois que o schema já existe. As migrations em si são
    /// aplicadas pela factory, que tem acesso ao DbContext configurado pela aplicação.
    /// </summary>
    public async Task InitializeRespawnerAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = new[] { "dbo" },
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
            WithReseed = true
        });
    }

    /// <summary>
    /// Apaga os dados de todas as tabelas respeitando a ordem das foreign keys e
    /// reinicia os contadores de identity, para que os ids sejam previsíveis.
    /// </summary>
    public async Task ResetAsync()
    {
        if (_respawner is null)
            throw new InvalidOperationException(
                $"Chame {nameof(InitializeRespawnerAsync)} antes de resetar o banco.");

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(
            new SqlConnectionStringBuilder(_serverConnectionString)
            {
                InitialCatalog = "master"
            }.ConnectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             IF DB_ID('{_databaseName}') IS NOT NULL
             BEGIN
                 ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                 DROP DATABASE [{_databaseName}];
             END
             """;

        await command.ExecuteNonQueryAsync();
    }
}
