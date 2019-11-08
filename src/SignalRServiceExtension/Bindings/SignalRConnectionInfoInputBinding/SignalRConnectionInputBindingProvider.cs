using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRConnectionInputBindingProvider : IBindingProvider
    {
        private IAccessTokenProvider accessTokenAccessTokenProvider;
        private SignalRConfigProvider signalRConfigProvider;
        private ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer;

        public SignalRConnectionInputBindingProvider(SignalRConfigProvider signalRConfigProvider, IAccessTokenProvider accessTokenProvider, ISignalRConnectionInfoConfigurer signalRConnectionInfoConfigurer)
        {
            this.accessTokenAccessTokenProvider = accessTokenProvider;
            this.signalRConfigProvider = signalRConfigProvider;
            this.signalRConnectionInfoConfigurer = signalRConnectionInfoConfigurer;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            // filter attribute
            var parameterInfo = context.Parameter;
            var attribute = parameterInfo.GetCustomAttribute<SignalRConnectionInfoAttribute>(false);
            if (attribute == null)
            {
                return Task.FromResult<IBinding>(null);
            }
            return Task.FromResult((IBinding) new SignalRConnectionInputBinding(attribute, signalRConfigProvider.GetAzureSignalRClient(attribute.ConnectionStringSetting, attribute.HubName), accessTokenAccessTokenProvider, signalRConnectionInfoConfigurer));
        }
    }
}
