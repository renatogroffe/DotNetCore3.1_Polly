using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Contrib.Simmy;
using WorkerConsumoAPIContagem.Clients;

namespace WorkerConsumoAPIContagem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Geração de uma mensagem simulado erro HTTP do tipo 503
                    var faultMessage = new HttpResponseMessage(
                        HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(
                            "Erro HTTP 503: Simulação de serviço indisponível com Simmy...")
                    };

                    // Configuração de uma falha a ser simulada via Simmy
                    var faultPolicy = MonkeyPolicy.InjectFaultAsync(
                        faultMessage,
                        injectionRate: 0.6,
                        enabled: () => true
                    );

                    // Configuração da Policy para Retry
                    var retryPolicy = Policy
                        .HandleResult<HttpResponseMessage>(
                            r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                        .RetryAsync(3, onRetry: (message, retryCount) =>
                        {
                            string msg = $"Retentativa: {retryCount}";

                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Out.WriteLineAsync(msg);
                            LogFileHelper.WriteMessage(msg);
                        });

                    var policyWrap = Policy.WrapAsync(retryPolicy, faultPolicy);

                    services.AddHttpClient<APIContagemClient>()
                        .AddPolicyHandler(policyWrap);
                    services.AddHostedService<Worker>();
                });
    }
}