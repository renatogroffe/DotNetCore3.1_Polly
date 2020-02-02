using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using Refit;
using CargaProdutos.Extensions;
using CargaProdutos.Interfaces;
using CargaProdutos.Models;

namespace CargaProdutos.Clients
{
    public class APIProdutosClient
    {
        private ILoginAPI _loginAPI;
        private IProdutosAPI _produtosAPI;
        private IConfiguration _configuration;
        private Token _token;
        private AsyncRetryPolicy _jwtPolicy;

        public bool IsAuthenticatedUsingToken
        {
            get => _token?.Authenticated ?? false;
        }

        public APIProdutosClient(IConfiguration configuration)
        {
            _configuration = configuration;
            string urlBase = _configuration.GetSection(
                "APIProdutos_Access:UrlBase").Value;

            _loginAPI = RestService.For<ILoginAPI>(urlBase);
            _produtosAPI = RestService.For<IProdutosAPI>(urlBase);
            _jwtPolicy = CreateAccessTokenPolicy();
        }

        public Task Autenticar()
        {
            try
            {
                // Envio da requisição a fim de autenticar
                // e obter o token de acesso
                _token = _loginAPI.PostCredentials(
                    new User()
                    {
                        UserID = _configuration.GetSection("APIProdutos_Access:UserID").Value,
                        Password = _configuration.GetSection("APIProdutos_Access:Password").Value
                    }).Result;
                return Console.Out.WriteLineAsync(JsonSerializer.Serialize(_token));
            }
            catch
            {
                _token = null;
                return Console.Out.WriteLineAsync("Falha ao autenticar...");
            }
        }

        private AsyncRetryPolicy CreateAccessTokenPolicy()
        {
            return Policy
                .HandleInner<ApiException>(
                    ex => ex.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(1, async (ex, retryCount, context) =>
                {
                    var corAnterior = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Console.Out.WriteLineAsync("Execução de RetryPolicy...");
                    Console.ForegroundColor = corAnterior;

                    await Autenticar();
                    if (!(_token?.Authenticated ?? false))
                        throw new InvalidOperationException("Token inválido!");

                    context["AccessToken"] = _token.AccessToken;
                });
        }

        public Task IncluirProduto(Produto produto)
        {
            var inclusao = _jwtPolicy.ExecuteWithTokenAsync<ResultadoAPIProdutos>(
                _token, async (context) =>
                {
                    var resultado = await _produtosAPI.IncluirProduto(
                        $"Bearer {context["AccessToken"]}",
                        produto);
                    return resultado;
                });

            return Console.Out.WriteLineAsync(
                JsonSerializer.Serialize(inclusao.Result));
        }

        public Task ListarProdutos()
        {
            var consulta = _jwtPolicy.ExecuteWithTokenAsync<List<Produto>>(
                _token, async (context) =>
            {
                var resultado = await _produtosAPI.ListarProdutos(
                  $"Bearer {context["AccessToken"]}");
                return resultado;
            });

            return Console.Out.WriteLineAsync("Produtos cadastrados: " +
                JsonSerializer.Serialize(consulta.Result));
        }
    }
}