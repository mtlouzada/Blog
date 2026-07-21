using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data;

public static class DbUpdateExceptionExtensions
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    /// <summary>
    /// Distingue "o cliente mandou um registro repetido" de "o banco recusou a
    /// escrita por outro motivo". Sem essa distinção, qualquer falha de gravação
    /// vira a mesma mensagem e o erro real fica invisível.
    /// </summary>
    public static bool IsUniqueViolation(this DbUpdateException exception)
        => exception.InnerException is SqlException sql
           && sql.Number is UniqueIndexViolation or UniqueConstraintViolation;
}
