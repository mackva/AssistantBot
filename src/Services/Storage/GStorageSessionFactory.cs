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

        public GStorageSessionFactory(IOptions<GStorageConfiguration> options, ILogger<GStorageSessionFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IGStorageSession OpenSession()
        {
            var driveService = GetDriveService();
            return new GStorageSession(driveService, _options, _logger);
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
