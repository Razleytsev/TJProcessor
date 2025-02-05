using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly;
using System.Net.Http.Json;

namespace TJConnector.StateSystem.Helpers;
public class CustomHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomHttpClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public CustomHttpClient(IHttpClientFactory httpClientFactory, ILogger<CustomHttpClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExternalApi");
        _logger = logger;

        // Retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} of {context.PolicyKey} due to {exception.Message}. Waiting {delay} before next retry.");
                });

        _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(15), TimeoutStrategy.Optimistic);

        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, breakDelay) =>
                {
                    _logger.LogWarning($"Circuit breaker opened. Will not attempt for {breakDelay.TotalSeconds} seconds.");
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset.");
                });
    }

    public async Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        var policyWrap = Policy.WrapAsync(_circuitBreakerPolicy, _timeoutPolicy, _retryPolicy);
        return await policyWrap.ExecuteAsync(() => _httpClient.GetAsync(requestUri));
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T data)
    {
        var policyWrap = Policy.WrapAsync(_circuitBreakerPolicy, _timeoutPolicy, _retryPolicy);
        return await policyWrap.ExecuteAsync(() => _httpClient.PostAsJsonAsync(requestUri, data));
    }
}