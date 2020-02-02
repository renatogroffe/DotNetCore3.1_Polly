using System;
using System.Collections.Generic;
using System.Net.Http;
using Polly;
using Polly.Retry;
using CargaProdutos.Models;

namespace CargaProdutos.Extensions
{
    public static class RetryPolicyExtensions
    {
        public static HttpResponseMessage ExecuteWithToken(
            this RetryPolicy<HttpResponseMessage> retryPolicy,
            Token token,
            Func<Context, HttpResponseMessage> action)
        {
            return retryPolicy.Execute(action,
                new Dictionary<string, object>
                {
                    { "AccessToken", token.AccessToken }
                });
        }
    }
}