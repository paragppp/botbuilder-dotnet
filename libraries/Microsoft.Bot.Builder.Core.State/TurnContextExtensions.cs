using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Core.State
{
    public static class TurnContextExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="IConversationStateManager"/> for the <paramref name="turnContext">context</paramref>.
        /// </summary>
        /// <param name="turnContext">The <see cref="ITurnContext"/> whose <see cref="ITurnContext.Activity">activity</see> will be used as the source for channel and conversation identifiers.</param>
        /// <returns></returns>
        public static IConversationStateManager ConversationState(this ITurnContext turnContext) =>
            GetOrCreateStateManager(
                turnContext,
                ConversationStateManager.PreferredStateStoreName,
                stateStore => new ConversationStateManager(turnContext.Activity.ChannelId, turnContext.Activity.Conversation.Id, stateStore));


        public static IUserStateManager UserState(this ITurnContext turnContext) =>
            GetOrCreateStateManager(
                turnContext,
                UserStateManager.PreferredStateStoreName,
                stateStore => new UserStateManager(turnContext.Activity.From.Id, stateStore));

        public static IStateManager State(this ITurnContext turnContext, string stateNamespace) =>
            GetOrCreateStateManager(
                turnContext,
                null,
                stateStore => new StateManager(stateNamespace, stateStore));


        private static TStateManager GetOrCreateStateManager<TStateManager>(ITurnContext turnContext, string preferredStateStoreServiceKey, Func<IStateStore, TStateManager> createStateManager) where TStateManager : class, IStateManager
        {
            var turnContextServices = turnContext.Services;
            var stateManager = turnContextServices.Get<TStateManager>();

            if (stateManager == default(TStateManager))
            {
                var stateStore = default(IStateStore);

                if (preferredStateStoreServiceKey != null)
                {
                    stateStore = turnContextServices.Get<IStateStore>(preferredStateStoreServiceKey);
                }

                if(stateStore == default(IStateStore))
                { 
                    stateStore = turnContextServices.Get<IStateStore>();
                }

                if (stateStore == default(IStateStore))
                {
                    throw new InvalidOperationException($"The state management feature requires an {nameof(IStateStore)} implementation to have been registered in the services collection, but none was found.");
                }

                stateManager = createStateManager(stateStore);

                turnContextServices.Add(stateManager);
            }

            return stateManager;
        }
    }
}
