namespace Microsoft.Bot.Builder.Core.State
{
    public interface IUserStateManager : IStateManager
    {
    }

    public class UserStateManager : StateManager, IUserStateManager
    {
        public UserStateManager(string userId, IStateStore stateStore) : base($"/users/{userId}", stateStore)
        {
        }
    }
}
