namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    internal static class StorageEntryOptionsExtensions
    {
        private const string AbsoluteExpirationKey = "AbsoluteExpiration";
        private const string SlidingExpirationKey = "SlidingExpiration";
        public static Dictionary<string, string> ToProperties(this StorageEntryOptions options)
        {
            var properties = new Dictionary<string, string>();

            if (options.AbsoluteExpiration.HasValue)
                properties[AbsoluteExpirationKey] = options.AbsoluteExpiration.Value.ToString();

            if (options.SlidingExpiration.HasValue)
                properties[SlidingExpirationKey] = options.SlidingExpiration.Value.ToString();

            return properties;
        }

        public static bool IsExpired(DateTime modifiedTime, IDictionary<string, string> properties)
        {
            if (properties.ContainsKey(AbsoluteExpirationKey) && DateTimeOffset.UtcNow > DateTimeOffset.Parse(properties[AbsoluteExpirationKey]))
                return true;

            if (properties.ContainsKey(SlidingExpirationKey))
                return DateTime.UtcNow > modifiedTime + TimeSpan.Parse(properties[SlidingExpirationKey]);

            return false;
        }

        public static StorageEntryOptions MakeStorageEntryOptions(IDictionary<string, string> properties)
        {
            var options = new StorageEntryOptions();

            if (properties.ContainsKey(AbsoluteExpirationKey))
                options.AbsoluteExpiration = DateTimeOffset.Parse(properties[AbsoluteExpirationKey]);

            if (properties.ContainsKey(SlidingExpirationKey))
                options.SlidingExpiration = TimeSpan.Parse(properties[SlidingExpirationKey]);

            return options;
        }
    }
}
