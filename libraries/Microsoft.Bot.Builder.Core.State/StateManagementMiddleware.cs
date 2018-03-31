﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.State
{
    public class StateManagementMiddleware : IMiddleware
    {
        public const string DefaultStateStoreName = nameof(StateManagementMiddleware) + ".DefaultStateStore";

        private Dictionary<string, IStateStore> _configuredStateStores = new Dictionary<string, IStateStore>();
        private Dictionary<string, StateManagerConfiguration> _configuredStateManagers = new Dictionary<string, StateManagerConfiguration>();

        private bool _autoLoadEnabled;
        private bool _autoLoadAll;
        private bool _autoSaveEnabled;
        private bool _autoSaveAll;

        public StateManagementMiddleware()
        {
        }

        public StateManagementMiddleware UseStateStore(string name, IStateStore stateStore)
        {
            _configuredStateStores[name] = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

            return this;
        }

        public StateManagementMiddleware UseDefaultStateStore(IStateStore stateStore) => UseStateStore(DefaultStateStoreName, stateStore);

        public StateManagementMiddleware UseStateManager(Type stateManagerType, Action<StateManagerConfigurationBuilder> configure = null)
        {
            if (stateManagerType == null)
            {
                throw new ArgumentNullException(nameof(stateManagerType));
            }

            if (stateManagerType.IsSubclassOf(typeof(IStateManager)))
            {
                throw new ArgumentException($"Specified type is not a subtype of {typeof(IStateManager).Name}.", nameof(stateManagerType));
            }

            if (stateManagerType == typeof(StateManager))
            {
                throw new ArgumentException($"Cannot register the standard {typeof(StateManager).Name}, only custom state managers.", nameof(stateManagerType));
            }

            return UseStateManager(new StateManagerConfiguration
            {
                StateNamespace = BuildStateManagerNamespaceForType(stateManagerType),
                StateManagerType = stateManagerType,
                StateStoreName = DefaultStateStoreName,
            },
            configure);
        }

        public StateManagementMiddleware UseStateManager(string stateNamespace, Action<StateManagerConfigurationBuilder> configure = null) =>
            UseStateManager(new StateManagerConfiguration
            {
                StateNamespace = stateNamespace,
                StateStoreName = DefaultStateStoreName,
            },
            configure);

        private StateManagementMiddleware UseStateManager(StateManagerConfiguration stateManagerConfiguration, Action<StateManagerConfigurationBuilder> configure = null)
        {
            if (stateManagerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(stateManagerConfiguration));
            }

            configure?.Invoke(new StateManagerConfigurationBuilder(stateManagerConfiguration));

            _configuredStateManagers[stateManagerConfiguration.StateNamespace] = stateManagerConfiguration;

            _autoLoadEnabled = stateManagerConfiguration.AutoLoad;

            return this;
        }

        public StateManagementMiddleware AutoLoadAll()
        {
            _autoLoadEnabled = true;
            _autoLoadAll = true;

            return this;
        }

        public StateManagementMiddleware AutoSaveAll()
        {
            _autoSaveEnabled = true;
            _autoSaveAll = true;

            return this;
        }

        public async Task OnProcessRequest(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            var contextServices = context.Services;
            var stateManagerResolver = new StateManagerServiceResolver(context, _configuredStateStores, _configuredStateManagers);

            AddStandardServicesToTurnContext();

            await AutoLoadIfEnabled();

            await next();

            await AutoSaveIfEnabled();

            void AddStandardServicesToTurnContext()
            {
                contextServices.Add<IStateManagerServiceResolver>(stateManagerResolver);

                foreach (var stateStoreEntry in _configuredStateStores)
                {
                    contextServices.Add(stateStoreEntry.Key, stateStoreEntry.Value);
                }
            }

            Task AutoLoadIfEnabled()
            {
                if (!_autoLoadEnabled)
                {
                    return Task.CompletedTask;
                }

                var loadTasks = new List<Task>();

                foreach (var stateManagerConfiguration in _configuredStateManagers.Values)
                {
                    if (_autoLoadAll || stateManagerConfiguration.AutoLoad)
                    {
                        var stateManager = stateManagerResolver.ResolveStateManager(stateManagerConfiguration);

                        var loadTask = default(Task);

                        if (_autoLoadAll || stateManagerConfiguration.AutoLoadSpecificKeys == null)
                        {
                            loadTask = stateManager.LoadAll();
                        }
                        else
                        {
                            loadTask = stateManager.Load(stateManagerConfiguration.AutoLoadSpecificKeys);
                        }

                        loadTasks.Add(loadTask);
                    }
                }

                return Task.WhenAll(loadTasks);
            }

            Task AutoSaveIfEnabled()
            {
                if (!_autoSaveEnabled)
                {
                    return Task.CompletedTask;
                }

                if (_autoSaveAll)
                {
                    var saveTasks = new List<Task>();

                    foreach (var stateManager in stateManagerResolver.ResolvedStateManagers)
                    {
                        saveTasks.Add(stateManager.SaveChanges());
                    }

                    return Task.WhenAll(saveTasks);
                }
                else
                {
                    throw new NotImplementedException("Partial auto-saving is not implemented yet.");
                }
            }
        }

        internal static string BuildStateManagerNamespaceForType(Type stateManagerType) => $"TYPED:{stateManagerType.FullName}";
    }

    public class StateManagerConfiguration
    {
        public string StateNamespace { get; set; }
        public Type StateManagerType { get; set; }
        public string StateStoreName { get; set; }
        public bool AutoLoad { get; set; }
        public string[] AutoLoadSpecificKeys { get; set; }
        public Func<ITurnContext, IStateStore, IStateManager> Factory { get; set; }
    }

    public interface IStateManagerServiceResolver
    {
        TStateManager ResolveStateManager<TStateManager>() where TStateManager : class, IStateManager;
        IStateManager ResolveStateManager(string stateNamespace);
    }

    public class StateManagerServiceResolver : IStateManagerServiceResolver
    {
        private readonly ITurnContext _turnContext;
        private readonly Dictionary<string, IStateStore> _availableStateStores;
        private readonly Dictionary<string, StateManagerConfiguration> _availableStateManagerConfigurations;
        private readonly Dictionary<string, IStateManager> _resolvedStateManagers = new Dictionary<string, IStateManager>();

        public StateManagerServiceResolver(ITurnContext turnContext, Dictionary<string, IStateStore> availableStateStores, Dictionary<string, StateManagerConfiguration> availableStateManagerConfigurations)
        {
            _turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            _availableStateStores = availableStateStores;
            _availableStateManagerConfigurations = availableStateManagerConfigurations;
        }

        public IReadOnlyCollection<IStateManager> ResolvedStateManagers => _resolvedStateManagers.Values;

        public IStateManager ResolveStateManager(string stateNamespace)
        {
            if (string.IsNullOrEmpty(stateNamespace))
            {
                throw new ArgumentException("Expected a non-null/empy value.", nameof(stateNamespace));
            }

            if (_resolvedStateManagers.TryGetValue(stateNamespace, out var stateManager))
            {
                return stateManager;
            }

            if (!_availableStateManagerConfigurations.TryGetValue(stateNamespace, out var stateManagerConfiguration))
            {
                throw new InvalidOperationException($"No state manager for namespace \"{stateNamespace}\" has been configured with the state management middleware. Please check your middleware configuration.");
            }

            return CreateAndCacheStateManager(stateManagerConfiguration);
        }

        public TStateManager ResolveStateManager<TStateManager>() where TStateManager : class, IStateManager
        {
            var typedStateManagerKey = StateManagementMiddleware.BuildStateManagerNamespaceForType(typeof(TStateManager));

            if (_resolvedStateManagers.TryGetValue(typedStateManagerKey, out var stateManager))
            {
                return stateManager as TStateManager;
            }

            if (!_availableStateManagerConfigurations.TryGetValue(typedStateManagerKey, out var stateManagerConfiguration))
            {
                throw new InvalidOperationException($"No state manager of type {typeof(TStateManager).Name} has been configured with the state management middleware. Please check your middleware configuration.");
            }

            return CreateAndCacheStateManager(stateManagerConfiguration) as TStateManager;
        }

        internal IStateManager ResolveStateManager(StateManagerConfiguration stateManagerConfiguration)
        {
            var stateManagerKey = stateManagerConfiguration.StateNamespace;

            if(_resolvedStateManagers.TryGetValue(stateManagerConfiguration.StateNamespace, out var stateManager))
            {
                return stateManager;
            }

            return CreateAndCacheStateManager(stateManagerConfiguration); ;
        }

        private IStateManager CreateAndCacheStateManager(StateManagerConfiguration stateManagerConfiguration)
        {
            if (stateManagerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(stateManagerConfiguration));
            }

            if (!_availableStateStores.TryGetValue(stateManagerConfiguration.StateStoreName, out var configuredStateStore))
            {
                if (stateManagerConfiguration.StateManagerType != null)
                {
                    throw new InvalidOperationException($"State manager of type \"{stateManagerConfiguration.StateManagerType.FullName}\" was configured to use a state store named \"{stateManagerConfiguration.StateStoreName}\", but no such named state store was configured. Please check your middleware configuration.");
                }
                else
                {
                    throw new InvalidOperationException($"State manager for namespace \"{stateManagerConfiguration.StateNamespace}\" was configured to use a state store named \"{stateManagerConfiguration.StateStoreName}\", but no such named state store was configured. Please check your middleware configuration.");
                }
            }

            var stateManager = default(IStateManager);

            if (stateManagerConfiguration.StateManagerType != null)
            {
                if (stateManagerConfiguration.Factory != null)
                {
                    stateManager = stateManagerConfiguration.Factory(_turnContext, configuredStateStore);
                }
                else
                {
                    try
                    {
                        stateManager = (IStateManager)Activator.CreateInstance(stateManagerConfiguration.StateManagerType, configuredStateStore);
                    }
                    catch (Exception createException)
                    {
                        throw new Exception($"Failed to create an instance of a typed state manager: {stateManagerConfiguration.StateManagerType.FullName}. Please check the inner exception for more details.", createException);
                    }
                }
            }
            else
            {
                stateManager = new StateManager(stateManagerConfiguration.StateNamespace, configuredStateStore);
            }

            _resolvedStateManagers.Add(stateManagerConfiguration.StateNamespace, stateManager);

            return stateManager;
        }
    }

    public class StateManagerConfigurationBuilder
    {
        internal StateManagerConfigurationBuilder(StateManagerConfiguration configuration)
        {
            Configuration = configuration;
        }

        private StateManagerConfiguration Configuration { get; }

        public StateManagerConfigurationBuilder UseStateStore(string stateStoreName)
        {
            if (string.IsNullOrEmpty(stateStoreName))
            {
                throw new ArgumentException("Expected a non-null/empty value.", nameof(stateStoreName));
            }

            Configuration.StateStoreName = stateStoreName;

            return this;
        }

        public StateManagerConfigurationBuilder UseFactory(Func<ITurnContext, IStateStore, IStateManager> stateManagerFactory)
        {
            Configuration.Factory = stateManagerFactory ?? throw new ArgumentNullException(nameof(stateManagerFactory));

            return this;
        }

        public StateManagerConfigurationBuilder AutoLoadAll()
        {
            Configuration.AutoLoad = true;
            Configuration.AutoLoadSpecificKeys = null;

            return this;
        }

        public StateManagerConfigurationBuilder AutoLoad(IEnumerable<string> keys) => AutoLoad(keys.ToArray());

        public StateManagerConfigurationBuilder AutoLoad(params string[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Length == 0)
            {
                throw new ArgumentException($"Expected one or more keys to be specified. Consider using {nameof(AutoLoadAll)} if you want all keys to be loaded.", nameof(keys));
            }

            Configuration.AutoLoad = true;
            Configuration.AutoLoadSpecificKeys = keys;

            return this;
        }
    }

    public static class StateManagementMiddlewareExtensions
    {
        public static StateManagementMiddleware UseState<TStateManager>(this StateManagementMiddleware stateManagementMiddleware, string storeName = null) where TStateManager : class, IStateManager =>
            stateManagementMiddleware.UseState<TStateManager>(storeName == null ? (Action<StateManagerConfigurationBuilder>)null : cb => cb.UseStateStore(storeName));

        public static StateManagementMiddleware UseState<TStateManager>(this StateManagementMiddleware stateManagementMiddleware, Action<StateManagerConfigurationBuilder> configure = null) where TStateManager : class, IStateManager =>
            stateManagementMiddleware.UseStateManager(typeof(TStateManager), configure);

        public static StateManagementMiddleware UseUserState(this StateManagementMiddleware stateManagementMiddleware, string storeName = null) =>
            stateManagementMiddleware.UseState<IUserStateManager>(cb =>
            {
                cb.UseFactory((tc, ss) => new UserStateManager(tc.Activity.From.Id, ss));

                if (storeName != null)
                {
                    cb.UseStateStore(storeName);
                }
            });

        public static StateManagementMiddleware UseConversationState(this StateManagementMiddleware stateManagementMiddleware, string storeName = null) =>
            stateManagementMiddleware.UseState<IConversationStateManager>(cb =>
            {
                cb.UseFactory((tc, ss) => new ConversationStateManager(tc.Activity.ChannelId, tc.Activity.Conversation.Id, ss));

                if (storeName != null)
                {
                    cb.UseStateStore(storeName);
                }
            });
    }


}
