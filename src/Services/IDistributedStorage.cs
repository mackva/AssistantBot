using System.IO;

namespace Telegram.Bot.Examples.WebHook.Services
{
    public interface IDistributedStorage
    {
        Task SetAsync(string id, byte[] value, CancellationToken token = default);
        Task<byte[]> GetAsync(string id, CancellationToken token = default);
        Task RemoveAsync(string id, CancellationToken token = default);
        Task SetAsync<T>(string id, T value, CancellationToken token = default);
        Task<T> GetAsync<T>(string id, CancellationToken token = default);
    }
}
