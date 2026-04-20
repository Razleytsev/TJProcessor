using TJConnector.Postgres.Entities;
using TJConnector.StateSystem.Model.ExternalResponses.Container;
using TJConnector.StateSystem.Model.ExternalResponses.Product;
using TJConnector.Web.Services.Contracts;

namespace TJConnector.Web.Services.Implementation;
public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/product");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<Product>>() ?? Array.Empty<Product>();
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/product/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>() ?? throw new InvalidOperationException("Product not found.");
    }

    public async Task<CustomResult<ProductInfoResponse>> GetExternalProductByIdAsync(string uuid)
    {
        var response = await _httpClient.GetAsync($"api/product/external/{uuid}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomResult<ProductInfoResponse>>() ?? throw new InvalidOperationException("External product data is null.");
    }

    public async Task<string> GetExternalProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/product/external");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync() ?? string.Empty;
    }
}