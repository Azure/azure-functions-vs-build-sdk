using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.NET.Sdk.Functions.Http
{
    internal class DefaultHttpClient : IHttpClient
    {
        private HttpClient _httpClient = new HttpClient();

        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        public Task<HttpResponseMessage> PostAsync(Uri uri, StreamContent content)
        {
            return _httpClient.PostAsync(uri, content);
        }
    }
}
