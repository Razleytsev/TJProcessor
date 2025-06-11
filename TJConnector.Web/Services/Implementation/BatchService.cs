using Microsoft.AspNetCore.Mvc;
using TJConnector.Postgres.Entities;
using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.SharedLibrary.Models;

namespace TJConnector.Web.Services.Implementation;
public interface IBatchServiceWeb
{
    Task<IEnumerable<Batch>> GetBatchesAsync();
    Task<BatchDTO> GetBatchByIdAsync(int id);
    Task<FileContentResult> DownloadBatchContentAsync(int id, string user);
    Task<Batch> ProcessBatchAsync(int id);
    Task<Batch> CancelBatchAsync(int id);
    Task<Batch> CreateBatchAsync(BatchCreateForm form);
}

public class BatchServiceWeb : IBatchServiceWeb
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BatchServiceWeb> _logger;

    public BatchServiceWeb(HttpClient httpClient, ILogger<BatchServiceWeb> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Batch>> GetBatchesAsync()
    {
        var response = await _httpClient.GetAsync("api/batch");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<Batch>>() ?? Array.Empty<Batch>();
    }

    public async Task<BatchDTO> GetBatchByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/batch/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BatchDTO>() ?? throw new InvalidOperationException("Batch not found.");
    }

    public async Task<FileContentResult> DownloadBatchContentAsync(int id, string user)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/batch/{id}/download?user={user}", new { });
        response.EnsureSuccessStatusCode();
        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        return new FileContentResult(fileBytes, "text/plain")
        {
            FileDownloadName = $"codes_{id}.txt"
        };
    }

    public async Task<Batch> ProcessBatchAsync(int id)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/batch/{id}/process", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Batch>() ?? throw new InvalidOperationException("Failed to process batch.");
    }

    public async Task<Batch> CancelBatchAsync(int id)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/batch/{id}/cancel", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Batch>() ?? throw new InvalidOperationException("Failed to cancel batch.");
    }

    public async Task<Batch> CreateBatchAsync(BatchCreateForm form)
    {
        var response = await _httpClient.PostAsJsonAsync("api/batch", form);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Batch>() ?? throw new InvalidOperationException("Failed to create batch.");
    }
}