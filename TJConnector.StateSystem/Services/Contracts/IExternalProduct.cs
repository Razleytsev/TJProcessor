using TJConnector.StateSystem.Model.ExternalRequests.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;

namespace TJConnector.StateSystem.Services.Contracts
{
    public interface IExternalProduct
    {
        Task<CustomResult<ProductInfoResponse>> GetProductByUUID(string uuid);
        Task<CustomResult<ProductPersonalListResponse>> GetProductPersonalList(ListRequestRequest listRequestBody);
    }
}
