using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.EntityFrameworkCore
{
    public enum EfCoreDatabaseProvider
    {
        SqlServer,
        MySql,
        Oracle,
        PostgreSql,
        Sqlite,
        InMemory,
        Cosmos,
        Firebird
    }
}
