namespace TJConnector.StateSystem.Helpers
{
    public class GetHttpClient(IHttpClientFactory httpClientFactory)
    {
        public HttpClient GetPublicHttpClient()
        {
            var client = httpClientFactory.CreateClient("ExternalApi");

            return client;
        }
    }
}
