namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    /// <summary>
    /// Provides methods to store and access data on disk with optional expiration
    /// </summary>
    public interface IGStorageSession : IDisposable
    {
        public Task CreateFileAsync(string name, Stream data, StorageEntryOptions options, CancellationToken token = default);
        public Task UpdateFileAsync(string name, Stream data, StorageEntryOptions options, CancellationToken token = default);
        public Task DownloadFileAsync(string name, Stream data, CancellationToken token = default);
        public Task<IEnumerable<StorageEntryOptions>> FindStorageOptionsAsync(string name, CancellationToken token = default);
        public Task DeleteAsync(string name, CancellationToken token = default);
    }
}
