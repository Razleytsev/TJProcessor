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
        Task<CodeOrder> CreateOrderAsync(OrderCreateForm form);
        Task<ProcessResponse> ProcessOrderAsync(int uuid);
        Task<CodeOrder> DownloadCodesAsync(int uuid);
        Task<IActionResult> DownloadOrderContentAsync(int id, string user);
        Task<CodeOrder> GetExternalOrderByIdAsync(int id); 
    }
}
