using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    public class GStorageSessionFactory : IGStorageSessionFactory
    {
        private readonly ILogger<GStorageSessionFactory> _logger;
        private readonly GStorageConfiguration _options;
        private Timer _timer;

        private string _id;

        public GStorageSessionFactory(IOptions<GStorageConfiguration> options, ILogger<GStorageSessionFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IGStorageSession OpenSession()
        {
            var driveService = GetDriveService();
            return new GStorageSession(driveService, _id, _options, _logger);
        }


        public async Task InitAsync(CancellationToken token = default)
        {
            using (var driveService = GetDriveService())
            {
                _id = await GStorageSession.InitAsync(driveService, _options, token);
            }
            
            _timer = new Timer(CleanupStorage, null, TimeSpan.Zero, TimeSpan.FromHours(24));
        }

        private async void CleanupStorage(object state)
        {
            _logger.LogDebug($"Cleaning up local store...");
            var driveService = GetDriveService();
            using (var session = new GStorageSession(driveService, _id, _options, _logger))
            {
                await session.CleanupStorageAsync();
            }

            _logger.LogDebug($"Local store cleaned up.");
        }

        private DriveService GetDriveService()
        {
            var scopes = new[] { DriveService.Scope.Drive };
            var credential = GoogleCredential.FromJson(_options.Credential).CreateScoped(scopes);

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _options.ApplicationName,
            });

            return service;
        }
    }
}
