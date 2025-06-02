using Microsoft.Data.Sqlite;

namespace TestTank.data;

public static class SqliteData
{
    static SqliteData()
    {
        var connection = new SqliteConnection("Data Source=Users.db");
        
    }
}