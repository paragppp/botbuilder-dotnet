namespace Microsoft.Bot.Builder.Core.State
{
    public interface IStateStoreEntry
    {
        string PartitionKey { get; }

        string Key { get;  }

        string ETag { get; }

        T GetValue<T>() where T : class;

        void SetValue<T>(T value) where T : class;
    }
}