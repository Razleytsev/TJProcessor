using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        public async Task<CustomResult<DbContainerContent>> GetContainerContent(string containerId)
        {
            using var connection = _connectionFactory.Create();
            {
                string sqlQuery = LoadSQLStatement("ContainerContentQuery.sql");
                var contentResults = await connection.QueryAsync<ContainerContent>(sqlQuery, new { Code = containerId });

                if (contentResults == null)
                    return new CustomResult<DbContainerContent> { Success = false, Message = $"No information for mastercases {containerId}" };

                var result = new DbContainerContent() { Content = contentResults.ToList() };

                return new CustomResult<DbContainerContent> { Content = result, Success = false, Message = "OK" };
            }
        }

        public async Task<CustomResult<DbContainerStatus>> GetContainerInfo(List<string> containerId)
        {
            using var connection = _connectionFactory.Create();
            {
                string sqlQuery = LoadSQLStatement("ContainerInfoQuery.sql");
                var statusResult = await connection.QueryAsync<ContainerStatus>(sqlQuery, new { Codes = containerId });
                if (statusResult == null)
                    return new CustomResult<DbContainerStatus> { Success = false, Message = $"Can't provide content for mastercase {containerId}" };

                var result = new DbContainerStatus() { Content = statusResult.ToList() };

                return new CustomResult<DbContainerStatus> { Content = result, Success = false, Message = "OK" };
            }
        }

        private string LoadSQLStatement(string statementName)
        {
            string sqlStatement = string.Empty;

            string namespacePart = "TJConnector.StateSystem";
            string resourceName = namespacePart + "." + statementName;

            using (Stream stm = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stm != null)
                {
                    sqlStatement = new StreamReader(stm).ReadToEnd();
                }
            }

            return sqlStatement;
        }
    }
}
