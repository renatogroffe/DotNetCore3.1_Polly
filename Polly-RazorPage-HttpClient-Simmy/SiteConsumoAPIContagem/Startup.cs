using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Contrib.Simmy;
using SiteConsumoAPIContagem.Clients;

namespace SiteConsumoAPIContagem
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
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

            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .RetryAsync(2, onRetry: (message, retryCount) =>
                {
                    string msg = $"Retentativa: {retryCount}";
                    Console.Out.WriteLineAsync(msg);
                    LogFileHelper.WriteMessage(msg);
                });

            var policyWrap = Policy.WrapAsync(retryPolicy, faultPolicy);

            services.AddHttpClient<APIContagemClient>()
                .AddPolicyHandler(policyWrap);

            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
