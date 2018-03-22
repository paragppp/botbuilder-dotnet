namespace Microsoft.Bot.Builder.Core.State
{
    public interface IConversationStateManager : IStateManager
    {
        string ChannelId { get; }
        string ConversationId { get; }
    }

    public class ConversationStateManager : StateManager, IConversationStateManager
    {
        public static string PreferredStateStoreName = "ConversationStateStore";

        public ConversationStateManager(string channelId, string conversationId, IStateStore stateStore) : base($"/channels/{channelId}/conversations/{conversationId}", stateStore)
        {
            ChannelId = channelId ?? throw new System.ArgumentNullException(nameof(channelId));
            ConversationId = conversationId ?? throw new System.ArgumentNullException(nameof(conversationId));
        }

        public string ChannelId { get; }

        public string ConversationId { get; }
    }
}
