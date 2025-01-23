using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;
using TJConnector.StateSystem.Model.ExternalResponses.MarkingCode;

namespace TJConnector.Web.Services.Contracts
{
    public interface IOrderService
    {
        Task<IEnumerable<CodeOrder>> GetOrdersAsync();
        Task<CodeOrder> GetOrderByIdAsync(int id);
        Task<CustomResult<DocumentCreateResponse>> CreateOrderAsync(OrderCreateForm form);
        Task<CustomResult<ProcessResponse>> ProcessOrderAsync(Guid uuid);
        Task<CustomResult<EmissionCodesResponse>> DownloadCodesAsync(Guid uuid);
        Task<IActionResult> DownloadOrderContentAsync(int id, string user);
    }
}
