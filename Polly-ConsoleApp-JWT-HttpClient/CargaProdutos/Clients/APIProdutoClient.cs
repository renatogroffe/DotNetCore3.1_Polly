using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using CargaProdutos.Models;
using CargaProdutos.Extensions;

namespace CargaProdutos.Clients
{
    public class APIProdutoClient
    {
        private HttpClient _client;
        private IConfiguration _configuration;
        private Token _token;
        private RetryPolicy<HttpResponseMessage> _jwtPolicy;

        public bool IsAuthenticatedUsingToken
        {
            get => _token?.Authenticated ?? false;
        }

        public APIProdutoClient(
            HttpClient client,
            IConfiguration configuration)
        {
            _client = client;
            _client.BaseAddress = new Uri(
                configuration.GetSection("APIProdutos_Access:UrlBase").Value);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _configuration = configuration;
            _jwtPolicy = CreateAccessTokenPolicy();
        }

        public void Autenticar()
        {
            // Envio da requisição a fim de autenticar
            // e obter o token de acesso
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, "login");
            requestMessage.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    UserID = _configuration.GetSection("APIProdutos_Access:UserID").Value,
                    Password = _configuration.GetSection("APIProdutos_Access:Password").Value
                }), Encoding.UTF8, "application/json");

            var respToken = _client.SendAsync(requestMessage).Result;

            string conteudo =
                respToken.Content.ReadAsStringAsync().Result;
            Console.WriteLine(conteudo);

            if (respToken.StatusCode == HttpStatusCode.OK)
                _token = JsonConvert.DeserializeObject<Token>(conteudo);
            else
                _token = null;
        }

        private RetryPolicy<HttpResponseMessage> CreateAccessTokenPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(
                    message => message.StatusCode == HttpStatusCode.Unauthorized)
                .Retry(1, (message, retryCount, context) =>
                {
                    Console.WriteLine("Execução de RetryPolicy...");

                    Autenticar();
                    if (!(_token?.Authenticated ?? false))
                        throw new InvalidOperationException("Token inválido!");

                    context["AccessToken"] = _token.AccessToken;
                });
        }

        public void IncluirProduto(Produto produto)
        {
            var response = _jwtPolicy.ExecuteWithToken(_token, context =>
            {
                Console.WriteLine("Iniciando IncluirProduto...");

                var requestMessage =
                    new HttpRequestMessage(HttpMethod.Post, "produtos");
                requestMessage.Headers.Add(
                    "Authorization", $"Bearer {context["AccessToken"]}");
                requestMessage.Content = new StringContent(
                    JsonConvert.SerializeObject(produto),
                    Encoding.UTF8, "application/json");

                return _client.SendAsync(requestMessage).Result;
            });

            Console.WriteLine(
                response.Content.ReadAsStringAsync().Result);
        }

        public List<Produto> ListarProdutos()
        {
            List<Produto> resultado = null;

            var response = _jwtPolicy.ExecuteWithToken(_token, context =>
            {
                Console.WriteLine("Iniciando ListarProdutos...");

                var requestMessage =
                    new HttpRequestMessage(HttpMethod.Get, "produtos");
                requestMessage.Headers.Add(
                    "Authorization", $"Bearer {context["AccessToken"]}");

                return _client.SendAsync(requestMessage).Result;
            });

            Console.WriteLine(
                response.Content.ReadAsStringAsync().Result);            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string conteudo = response.Content.ReadAsStringAsync().Result;
                resultado = JsonConvert.DeserializeObject<List<Produto>>(conteudo);
            }

            return resultado;
        }
    }
}