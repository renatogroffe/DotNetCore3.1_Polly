using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SiteConsumoAPIContagem.Clients
{
    public class APIContagemClient
    {
        private HttpClient _client;
        private IConfiguration _configuration;
        private ILogger<APIContagemClient> _logger;
        

        public APIContagemClient(
            HttpClient client, IConfiguration configuration,
            ILogger<APIContagemClient> logger)
        {
            _client = client;
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _configuration = configuration;
            _logger = logger;
        }

        public string ObterDadosContagem()
        {
            var response = _client.GetAsync(
                _configuration.GetSection("UrlAPIContagem").Value).Result;

            string resultado =
                response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Resultado normal: " + resultado);
            else
                _logger.LogError("Erro: " + resultado);
            LogFileHelper.WriteMessage(resultado);

            response.EnsureSuccessStatusCode();
            return resultado;
        }
    }
}