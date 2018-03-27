using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public sealed class StateManagerMiddleware : IMiddleware
    {
        public StateManagerMiddleware()
        {
        }

        public StateManagerMiddleware(bool enableAutoLoad, bool enableAutoSave)
        {
            EnableAutoLoad = enableAutoLoad;
            EnableAutoSave = enableAutoSave;
        }        

        public bool EnableAutoLoad { get; set; }
        public bool EnableAutoSave { get; set; }

        public async Task OnProcessRequest(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            if(EnableAutoLoad)
            {
                await DoForAllStateManagers(context, sm => sm.LoadAll());
            }

            await next();

            if(EnableAutoSave)
            {
                await DoForAllStateManagers(context, sm => sm.SaveChanges());
            }
        }

        public Task DoForAllStateManagers(ITurnContext context, Func<IStateManager, Task> action)
        {
            var tasks = new List<Task>();

            foreach (var stateManagerServiceEntry in context.Services.GetServices<IStateManager>())
            {
                tasks.Add(action(stateManagerServiceEntry.Value));
            }

            return Task.WhenAll(tasks);
        }
    }
}
