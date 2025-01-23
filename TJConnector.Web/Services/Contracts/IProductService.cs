using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;

namespace TJConnector.Web.Services.Contracts
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<CustomResult<ProductInfoResponse>> GetExternalProductByIdAsync(string uuid);
        Task<string> GetExternalProductsAsync();
    }
}
