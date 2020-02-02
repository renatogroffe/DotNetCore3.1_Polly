using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerConsumoAPIContagem.Clients;

namespace WorkerConsumoAPIContagem
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private APIContagemClient _client;

        public Worker(ILogger<Worker> logger,
            APIContagemClient client)
        {
            _logger = logger;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _client.EnviarRequisicao();
                }
                catch
                {
                    string mensagem = "Esgotadas as tentativas com Polly...";
                    _logger.LogError(mensagem);
                    LogFileHelper.WriteMessage(mensagem);
                }

                await Task.Delay(4000, stoppingToken);
            }
        }
    }
}