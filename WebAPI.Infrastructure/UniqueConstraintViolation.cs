using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace WebAPI.Infrastructure;

public static class UniqueConstraintViolation
{
    public static bool Is(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;

            if (current is SqliteException sqlite && sqlite.SqliteExtendedErrorCode == 2067)
                return true;
        }

        return false;
    }
}
