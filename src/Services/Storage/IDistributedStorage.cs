using System.IO;

namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    public interface IDistributedStorage
    {
        Task SetAsync(string id, byte[] value, StorageEntryOptions options, CancellationToken token = default);
        Task<byte[]> GetAsync(string id, CancellationToken token = default);
        Task RemoveAsync(string id, CancellationToken token = default);
        Task SetAsync<T>(string id, T value, StorageEntryOptions options, CancellationToken token = default);
        Task<T> GetAsync<T>(string id, CancellationToken token = default);
    }
}
