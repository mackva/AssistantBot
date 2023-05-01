namespace Telegram.Bot.Examples.WebHook.Services
{
    /// <summary>
    /// Provides methods to store and access data on disk with optional expiration
    /// </summary>
    public interface IGStorage
    {
        public Task InitAsync(CancellationToken token = default);
        public Task CreateFileAsync(string name, Stream data, CancellationToken token = default);
        public Task UpdateFileAsync(string name, Stream data, CancellationToken token = default);
        public Task DownloadFileAsync(string name, Stream data, CancellationToken token = default);
        public Task<bool> Exists(string name, CancellationToken token = default);
        public Task<IEnumerable<string>> GetFileIdsAsync(CancellationToken token = default);
        public Task DeleteAsync(string name, CancellationToken token = default);
    }
    public class FailedOperationException : Exception
    {
        public FailedOperationException() : base() { }
        public FailedOperationException(string message) : base(message) { }
        public FailedOperationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class GStorageConfiguration
    {
        public const string Key = "GStorageConfiguration";

        public string Credential { get; set; }
        public string ApplicationName { get; set; }
        public string StorageName { get; set; }
        public string ShareTo { get; set; }
    }
}
