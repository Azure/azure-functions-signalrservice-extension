using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable CS0618 // Type or member is obsolete
    internal class FunctionAuthorizeAttribute: FunctionInvocationFilterAttribute
    {
        private const string AdminKey = "admin";

        public override Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            var invocationContext = executingContext.Arguments.FirstOrDefault().Value as InvocationContext;
            if (invocationContext != null)
            {
                if (invocationContext.Claims.TryGetValue(AdminKey, out var value) &&
                    bool.TryParse(value, out var isAdmin) &&
                    isAdmin)
                {
                    return Task.CompletedTask;
                }
            }
            throw new InvalidOperationException();
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
