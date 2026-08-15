using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Sygnia.Infrastructure.Persistence;

internal static class SqlExceptionExtensions
{
    // 2627: violation of PRIMARY KEY constraint. 2601: cannot insert duplicate key row in a
    // unique index. Either means the (AccountId, ExternalRef) composite key already exists —
    // the idempotency mechanism the root CLAUDE.md describes. Caught here at the
    // infrastructure boundary; it must never escape as an exception.
    private static readonly int[] DuplicateKeyErrorNumbers = [2627, 2601];

    public static bool IsDuplicateKeyViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && DuplicateKeyErrorNumbers.Contains(sqlException.Number);
}
