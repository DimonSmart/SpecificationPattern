using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DimonSmart.Specification.EntityFrameworkCore.Db.Tests;

internal class EFCoreSqlLoggingInterceptor : DbCommandInterceptor
{
    private readonly IList<string> _dbCommandsLog;

    public EFCoreSqlLoggingInterceptor(IList<string> dbCommandsLog)
    {
        _dbCommandsLog = dbCommandsLog;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _dbCommandsLog.Add(command.CommandText);
        return result;
    }
}