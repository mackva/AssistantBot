using System.Text.Json;

namespace Telegram.Bot.Examples.WebHook.Services
{
    public class DistributedStorage : IDistributedStorage
    {
        private readonly IGStorage _storage;
        public DistributedStorage(IGStorage storage)
        {
            _storage = storage;
        }

        public async Task<byte[]> GetAsync(string id, CancellationToken token = default)
        {
            using(var stream = new MemoryStream())
            {
                await _storage.DownloadFileAsync(id, stream, token);
                return stream.ToArray();
            }
        }

        public async Task<T> GetAsync<T>(string id, CancellationToken token = default)
        {
            using (var stream = new MemoryStream())
            {
                await _storage.DownloadFileAsync(id, stream, token);
                return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: token);
            }
        }

        public async Task RemoveAsync(string id, CancellationToken token = default)
        {
            await _storage.DeleteAsync(id, token);
        }

        public async Task SetAsync(string id, byte[] value, CancellationToken token = default)
        {
            var exists = await _storage.Exists(id, token);
            if(exists)
            {
                using (var stream = new MemoryStream(value))
                {
                    await _storage.UpdateFileAsync(id, stream, token);
                }
            }
            else
            {
                using (var stream = new MemoryStream(value))
                {
                    await _storage.CreateFileAsync(id, stream, token);
                }
            }
        }

        public async Task SetAsync<T>(string id, T value, CancellationToken token = default)
        {
            var exists = await _storage.Exists(id, token);
            if (exists)
            {
                var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                using (var stream = new MemoryStream(jsonUtf8Bytes))
                {
                    await _storage.UpdateFileAsync(id, stream, token);
                }
            }
            else
            {
                var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                using (var stream = new MemoryStream(jsonUtf8Bytes))
                {
                    await _storage.CreateFileAsync(id, stream, token);
                }
            }
        }
    }
}
