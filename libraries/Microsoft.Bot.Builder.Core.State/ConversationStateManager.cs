namespace Microsoft.Bot.Builder.Core.State
{
    public interface IConversationStateManager : IStateManager
    {
    }

    public class ConversationStateManager : StateManager, IConversationStateManager
    {
        public ConversationStateManager(string channelId, string conversationId, IStateStore stateStore) : base($"/channels/{channelId}/conversations/{conversationId}", stateStore)
        {
        }
    }
}
