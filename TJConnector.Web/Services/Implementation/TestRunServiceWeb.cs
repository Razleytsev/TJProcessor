using TJConnector.SharedLibrary.DTOs.Forms;
using TJConnector.SharedLibrary.Models;

namespace TJConnector.Web.Services.Implementation;

public interface ITestRunServiceWeb
{
    Task<List<TestRunDto>> ListAsync();
    Task<TestRunDto?> GetByIdAsync(int id);
    Task<TestRunDto?> CreateAsync(TestRunCreateForm form);
    Task<TestRunDto?> ReprocessAsync(int parentId, int fromStage);
    Task<TestRunDto?> CancelAsync(int id);
}

public class TestRunServiceWeb : ITestRunServiceWeb
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TestRunServiceWeb> _logger;

    public TestRunServiceWeb(HttpClient httpClient, ILogger<TestRunServiceWeb> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TestRunDto>> ListAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/testrun");
            if (!response.IsSuccessStatusCode) return new List<TestRunDto>();
            return await response.Content.ReadFromJsonAsync<List<TestRunDto>>() ?? new List<TestRunDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list test runs");
            return new List<TestRunDto>();
        }
    }

    public async Task<TestRunDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/testrun/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TestRunDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get test run {Id}", id);
            return null;
        }
    }

    public async Task<TestRunDto?> CreateAsync(TestRunCreateForm form)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/testrun", form);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create test run: {Code} {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<TestRunDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test run");
            return null;
        }
    }

    public async Task<TestRunDto?> ReprocessAsync(int parentId, int fromStage)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/testrun/{parentId}/reprocess/{fromStage}", new { });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TestRunDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reprocess test run {Id} from stage {Stage}", parentId, fromStage);
            return null;
        }
    }

    public async Task<TestRunDto?> CancelAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/testrun/{id}/cancel", new { });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TestRunDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel test run {Id}", id);
            return null;
        }
    }
}
