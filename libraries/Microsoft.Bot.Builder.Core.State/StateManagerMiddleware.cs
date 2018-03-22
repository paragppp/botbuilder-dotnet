using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public sealed class StateManagerAutoLoaderMiddleware : IMiddleware
    {
        public StateManagerAutoLoaderMiddleware(bool enableAutoLoad, bool enableAutoSave)
        {
            EnableAutoLoad = enableAutoLoad;
            EnableAutoSave = enableAutoSave;
        }

        public bool EnableAutoLoad { get; }
        public bool EnableAutoSave { get; }

        public async Task OnProcessRequest(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            var stateManagerServices = context.Services.GetServices<IStateManager>().ToList();

            if(EnableAutoLoad && stateManagerServices.Count > 0)
            {
                await Task.WhenAll(stateManagerServices.Select(sms => sms.Value.Load()));
            }

            await next();

            if(EnableAutoSave && stateManagerServices.Count > 0)
            {
                await Task.WhenAll(stateManagerServices.Select(sms => sms.Value.SaveChanges()));
            }
        }
    }
}
