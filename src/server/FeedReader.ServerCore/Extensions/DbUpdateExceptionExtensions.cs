using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FeedReader.ServerCore
{
    public static class DbUpdateExceptionExtensions
    {
        public static bool IsUniqueConstraintException(this DbUpdateException ex)
        {
            /// Ref: https://www.postgresql.org/docs/8.2/errcodes-appendix.html
            var pgsqlException = ex.InnerException as NpgsqlException;
            return pgsqlException?.SqlState == "23505";
        }
    }
}
