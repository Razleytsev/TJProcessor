using Dapper;
using Microsoft.Extensions.Logging;
using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Helpers;
using TJConnector.StateSystem.Model.ExternalDbResult;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Services.Contracts;

namespace TJConnector.StateSystem.Services.Implementation
{
    public class ExternalDbData : IExternalDBData
    {
        private readonly ILogger<ExternalDbData> _logger;
        private readonly ISQLConnectionFactory _connectionFactory;

        public ExternalDbData(ILogger<ExternalDbData> logger, ISQLConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }
        public async Task<CustomResult<List<PackageContent>>> GetContainerContent(string containerId)
        {
            try
            {
                using var connection = _connectionFactory.Create();
                {
                    string sqlQuery = LoadSQLStatement("ContainerContentQuery.sql");
                    var contentResults = await connection.QueryAsync<ContainerContent>(sqlQuery, new { Code = containerId });

                    if (contentResults == null)
                        return new CustomResult<List<PackageContent>>
                        { Success = false, Message = $"No information for mastercases {containerId}" };

                    var result = contentResults
                        .GroupBy(i => i.Bundle)
                        .Select(g => new PackageContent
                        {
                            Bundle = g.Key,
                            Packs = g.Select(i => i.Pack).ToArray()
                        })
                        .ToList();

                    return new CustomResult<List<PackageContent>> { Content = result, Success = true, Message = "OK" };
                }
            }
            catch (Exception ex)
            {
                return new CustomResult<List<PackageContent>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomResult<DbContainerStatus>> GetContainerInfo(List<string> containerId)
        {
            try
            {
                using var connection = _connectionFactory.Create();
                {
                    string sqlQuery = LoadSQLStatement("ContainerInfoQuery.sql");
                    var parameters = containerId
                            .Select(x => new DbString
                            {
                                Value = x,
                                IsAnsi = true
                            });
                    var statusResult =
                        await connection.QueryAsync<ContainerStatus>(sqlQuery, new { Codes = parameters });
                    if (statusResult == null)
                        return new CustomResult<DbContainerStatus> { Success = false, Message = $"Can't provide content for mastercase {containerId}" };

                    var result = new DbContainerStatus() { Content = statusResult.ToList() };

                    return new CustomResult<DbContainerStatus> { Content = result, Success = true, Message = "OK" };
                }
            }
            catch (Exception ex)
            {
                return new CustomResult<DbContainerStatus> { Success = false, Message = ex.Message };
            }
        }

        private string LoadSQLStatement(string statementName)
        {
            string sqlStatement = string.Empty;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, statementName);

            if (!File.Exists(filePath))
            {
                sqlStatement = $"--placeholder. put here your query. for {statementName}\nSELECT * FROM Table;";
                File.WriteAllText(filePath, sqlStatement);
            }
            else
            {
                sqlStatement = File.ReadAllText(filePath);
            }
            return sqlStatement;
        }
    }
}
