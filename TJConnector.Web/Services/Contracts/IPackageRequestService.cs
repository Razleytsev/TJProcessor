using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.StateSystem.Model.ExternalResponses.Generic;

namespace TJConnector.Web.Services.Contracts
{
    public interface IPackageRequestService
    {
        Task<IEnumerable<PackageRequest>> GetPackageRequestsAsync();
        Task<PackageRequest> GetPackageRequestByIdAsync(int id);
        Task<PackageRequest> CreatePackageRequestAsync(PackageRequestForm form);
        Task<ProcessResponse> ProcessPackageRequestAsync(int uuid);
        Task<int> ReprocessPackage(int id);
    }
}
