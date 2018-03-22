namespace Microsoft.Bot.Builder.Core.State
{
    public interface IUserStateManager : IStateManager
    {
        string UserId { get; }
    }

    public class UserStateManager : StateManager, IUserStateManager
    {
        public static string PreferredStateStoreName = "UserStateStore";


        public UserStateManager(string userId, IStateStore stateStore) : base($"/users/{userId}", stateStore)
        {
            UserId = userId ?? throw new System.ArgumentNullException(nameof(userId));
        }

        public string UserId { get; }
    }
}
