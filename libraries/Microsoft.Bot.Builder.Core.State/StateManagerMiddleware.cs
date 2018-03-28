using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public sealed class StateManagerMiddleware : IMiddleware
    {
        private bool _autoLoadEnabled;
        private List<Func<ITurnContext, string>> _autoLoadStateManagerNameProviders;
        private bool _autoSaveEnabled;
        private List<Func<ITurnContext, string>> _autoSaveStateManagerNameProviders;

        public StateManagerMiddleware()
        {
        }

        public StateManagerMiddleware(bool autoLoadAll, bool autoSaveAll)
        {
            if (autoLoadAll)
            {
                AutoLoadAll();
            }

            if (autoSaveAll)
            {
                AutoSaveAll();
            }
        }

        public StateManagerMiddleware AutoLoadAll()
        {
            _autoLoadEnabled = true;
            _autoLoadStateManagerNameProviders = null;

            return this;
        }

        public StateManagerMiddleware AutoSaveAll()
        {
            _autoSaveEnabled = true;
            _autoSaveStateManagerNameProviders = null;

            return this;
        }

        public StateManagerMiddleware AutoLoad(IEnumerable<Func<ITurnContext, string>> stateManagerNameProviders)
        {
            _autoLoadEnabled = true;

            if (_autoLoadStateManagerNameProviders == null)
            {
                _autoLoadStateManagerNameProviders = new List<Func<ITurnContext, string>>();
            }

            foreach (var stateManagerNameProvider in stateManagerNameProviders)
            {
                _autoLoadStateManagerNameProviders.Add(stateManagerNameProvider);
            }

            return this;
        }

        public StateManagerMiddleware AutoSave(IEnumerable<Func<ITurnContext, string>> stateManagerNameProviders)
        {
            _autoSaveEnabled = true;

            if (_autoSaveStateManagerNameProviders == null)
            {
                _autoSaveStateManagerNameProviders = new List<Func<ITurnContext, string>>();
            }

            foreach (var stateManagerNameProvider in stateManagerNameProviders)
            {
                _autoSaveStateManagerNameProviders.Add(stateManagerNameProvider);
            }

            return this;
        }

        public async Task OnProcessRequest(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            if (_autoLoadEnabled)
            {
                await DoForAllStateManagers(context, _autoLoadStateManagerNameProviders, sm => sm.LoadAll());
            }

            await next();

            if(_autoSaveEnabled)
            {
                await DoForAllStateManagers(context, _autoSaveStateManagerNameProviders, sm => sm.SaveChanges());
            }
        }

        private Task DoForAllStateManagers(ITurnContext context, List<Func<ITurnContext, string>> specificStateManagerNameProviders, Func<IStateManager, Task> action)
        {
            var tasks = new List<Task>();

            var services = context.Services;
            var specificStateManagerNames = default(HashSet<string>);

            if (specificStateManagerNameProviders != null)
            {
                specificStateManagerNames = new HashSet<string>();

                foreach (var stateManagerNameProvider in specificStateManagerNameProviders)
                {
                    var specificStateManagerName = stateManagerNameProvider(context);

                    if (specificStateManagerName != null)
                    {
                        specificStateManagerNames.Add(specificStateManagerName);
                    }
                }
            }

            foreach (var stateManagerServiceEntry in services.GetServices<IStateManager>())
            {
                if(specificStateManagerNames == null || specificStateManagerNames.Contains(stateManagerServiceEntry.Key))
                {
                    tasks.Add(action(stateManagerServiceEntry.Value));
                }
            }

            return Task.WhenAll(tasks);
        }       
    }

    public static class StateManagerMiddlewareExtensions
    {
        public static StateManagerMiddleware AutoLoad(this StateManagerMiddleware stateManagerMiddleware, params Func<ITurnContext, string>[] stateManagerNameProviders) => stateManagerMiddleware.AutoLoad((IEnumerable<Func<ITurnContext, string>>)stateManagerNameProviders);
        public static StateManagerMiddleware AutoSave(this StateManagerMiddleware stateManagerMiddleware, params Func<ITurnContext, string>[] stateManagerNameProviders) => stateManagerMiddleware.AutoSave((IEnumerable<Func<ITurnContext, string>>)stateManagerNameProviders);
    }
}
