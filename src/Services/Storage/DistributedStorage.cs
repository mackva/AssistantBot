using System.Text.Json;

namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    public class DistributedStorage : IDistributedStorage
    {
        private readonly IGStorageSessionFactory _sessionFactory;
        public DistributedStorage(IGStorageSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public async Task<byte[]> GetAsync(string id, CancellationToken token = default)
        {
            using (var session = _sessionFactory.OpenSession())
            using (var stream = new MemoryStream())
            {
                await session.DownloadFileAsync(id, stream, token);
                return stream.ToArray();
            }
        }

        public async Task<T?> GetAsync<T>(string id, CancellationToken token = default)
        {
            using (var session = _sessionFactory.OpenSession())
            using (var stream = new MemoryStream())
            {
                await session.DownloadFileAsync(id, stream, token);
                return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: token);
            }
        }

        public async Task RemoveAsync(string id, CancellationToken token = default)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                await session.DeleteAsync(id, token);
            }
        }

        public async Task SetAsync(string id, byte[] value, StorageEntryOptions options, CancellationToken token = default)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                var storageOptions = await session.FindStorageOptionsAsync(id, token);
                if (storageOptions.Any())
                {
                    using (var stream = new MemoryStream(value))
                    {
                        await session.UpdateFileAsync(id, stream, options, token);
                    }
                }
                else
                {
                    using (var stream = new MemoryStream(value))
                    {
                        await session.CreateFileAsync(id, stream, options, token);
                    }
                }
            }
        }

        public async Task SetAsync<T>(string id, T value, StorageEntryOptions options, CancellationToken token = default)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                var storageOptions = await session.FindStorageOptionsAsync(id, token);
                if (storageOptions.Any())
                {
                    var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                    using (var stream = new MemoryStream(jsonUtf8Bytes))
                    {
                        await session.UpdateFileAsync(id, stream, options, token);
                    }
                }
                else
                {
                    var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                    using (var stream = new MemoryStream(jsonUtf8Bytes))
                    {
                        await session.CreateFileAsync(id, stream, options, token);

                    }
                }
            }
        }
    }
}
