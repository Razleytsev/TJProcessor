using TJConnector.Postgres.Entities;

namespace TJConnector.Web.Services.Contracts
{
    public interface IMetadataService
    {
        Task<IEnumerable<Factory>> GetFactoriesAsync();
        Task<IEnumerable<MarkingLine>> GetMarkingLinesAsync();
        Task<IEnumerable<Location>> GetLocationsAsync();
    }
}
