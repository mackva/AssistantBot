namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    public interface IGStorageSessionFactory
    {
        public IGStorageSession OpenSession();
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
