using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TJConnector.StateSystem.Helpers;

public sealed class SQLConnectionFactory : ISQLConnectionFactory
{
    private readonly string _connectionString;

    public SQLConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ExternalDb") ??
            throw new ApplicationException("ExternalDb connection string is missing");
    }
    public IDbConnection Create()
    {
        return new SqlConnection(_connectionString);
    }
}