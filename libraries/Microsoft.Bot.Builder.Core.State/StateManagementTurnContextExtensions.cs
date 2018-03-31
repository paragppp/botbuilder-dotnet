using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Core.State
{
    public static class StateManagementTurnContextExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="IConversationStateManager"/> for the <paramref name="turnContext">context</paramref>.
        /// </summary>
        /// <param name="turnContext">The <see cref="ITurnContext"/> whose <see cref="ITurnContext.Activity">activity</see> will be used as the source for channel and conversation identifiers.</param>
        /// <returns></returns>
        public static IConversationStateManager ConversationState(this ITurnContext turnContext) =>
            turnContext.Services.Get<IStateManagerServiceResolver>().ResolveStateManager<IConversationStateManager>();

        public static IUserStateManager UserState(this ITurnContext turnContext) =>
            turnContext.Services.Get<IStateManagerServiceResolver>().ResolveStateManager<IUserStateManager>();

        public static TStateManager State<TStateManager>(this ITurnContext turnContext) where TStateManager : class, IStateManager =>
            turnContext.Services.Get<IStateManagerServiceResolver>().ResolveStateManager<TStateManager>();

        public static IStateManager State(this ITurnContext turnContext, string stateNamespace) =>
            turnContext.Services.Get<IStateManagerServiceResolver>().ResolveStateManager(stateNamespace);
    }
}
