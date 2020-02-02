using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using CargaProdutos.Models;
using CargaProdutos.Clients;

namespace CargaProdutos
{
    class Program
    {
        private static void Interromper()
        {
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile($"appsettings.json");
            var config = builder.Build();

            using (var client = new HttpClient())
            {
                var apiProdutoClient =
                    new APIProdutoClient(client, config);
                apiProdutoClient.Autenticar();

                if (apiProdutoClient.IsAuthenticatedUsingToken)
                {
                    apiProdutoClient.IncluirProduto(
                        new Produto()
                        {
                            CodigoBarras = "00003",
                            Nome = "Teste Produto 03",
                            Preco = 30.33
                        });

                    Interromper();

                    apiProdutoClient.IncluirProduto(
                        new Produto()
                        {
                            CodigoBarras = "00004",
                            Nome = "Teste Produto 04",
                            Preco = 44.04
                        });

                    Interromper();

                    Console.WriteLine("Produtos cadastrados: " +
                        JsonConvert.SerializeObject(
                            apiProdutoClient.ListarProdutos()));
                }
            }

            Console.WriteLine("\nFinalizado!");
        }
    }
}