using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Download;
using Microsoft.Extensions.Options;

namespace Telegram.Bot.Examples.WebHook.Services
{
    public class GStorage : IGStorage
    {
        private string _folderId;
        private readonly ILogger<GStorage> _logger;
        private readonly GStorageConfiguration _options;

        public GStorage(IOptions<GStorageConfiguration> options, ILogger<GStorage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task InitAsync(CancellationToken token = default)
        {
            using (var service = GetService())
            {
                var folders = await GetResourcesAsync(service, "mimeType='application/vnd.google-apps.folder' and 'root' in parents", token);
                var storageFolder = folders.Where(folder => folder.Name == _options.StorageName).FirstOrDefault();
                if (storageFolder != null)
                {
                    _folderId = storageFolder.Id;
                }
                else
                {
                    var folderId = await CreateFolderAsync(service, _options.StorageName, token: token);
                    _folderId = folderId;
                    if(!string.IsNullOrEmpty(_options.ShareTo))
                    {
                        await ShareAsync(service, folderId, _options.ShareTo, token);
                    }
                }
            } 
        }

        public async Task CreateFileAsync(string name, Stream data, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (data == null)
                throw new ArgumentException(nameof(data));

            using (var service = GetService())
            {
                await CreateFileAsync(service, name, data, _folderId, token);
            }
        }

        public async Task UpdateFileAsync(string name, Stream data, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (data == null)
                throw new ArgumentException(nameof(data));

            using (var service = GetService())
            {
                var filses = await GetResourcesAsync(service, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents");
                var file = filses.Where(f => f.Name == name).FirstOrDefault();
                
                if (file == null)
                    throw new NotFoundException();

                await UpdateFileAsync(service, file.Id, data, token);
            }
        }

        public async Task DownloadFileAsync(string name, Stream data, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            using (var service = GetService())
            {
                var filses = await GetResourcesAsync(service, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents");
                var file = filses.Where(f => f.Name == name).FirstOrDefault();

                if (file == null)
                    throw new NotFoundException();

                await DownloadFileAsync(service, file.Id, data, token);             
            }
        }

        public async Task<IEnumerable<string>> GetFileIdsAsync(CancellationToken token = default)
        {
            using (var service = GetService())
            {
                var filses = await GetResourcesAsync(service, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents");
                return filses.Select(f => f.Id).ToList();
            }
        }

        public async Task DeleteAsync(string name, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            using (var service = GetService())
            {
                var filses = await GetResourcesAsync(service, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents");
                var file = filses.Where(f => f.Name == name).FirstOrDefault();              
                
                if (file == null)
                    throw new NotFoundException();

                await DeleteAsync(service, file.Id, token);
            }
        }

        public async Task<bool> Exists(string name, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            using (var service = GetService())
            {
                var filses = await GetResourcesAsync(service, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents");
                var file = filses.Where(f => f.Name == name).FirstOrDefault();

                if (file == null)
                    return false;

                return true;
            }
        }

        private DriveService GetService()
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

        private async Task DownloadFileAsync(DriveService service, string fileId, Stream stream, CancellationToken token = default)
        {
            var request = service.Files.Get(fileId);
            var results = await request.DownloadAsync(stream, token);

            if (results.Status == DownloadStatus.Failed)
            {
                _logger.LogError($"Error downloing file: {results.Exception.Message}");
                throw new FailedOperationException(results.Exception.Message, results.Exception);
            }
        }

        private async Task UpdateFileAsync(DriveService service, string id, Stream data, CancellationToken token = default)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File();

            var request = service.Files.Update(fileMetadata, id, data, "text/plain");
            request.Fields = "*";

            var results = await request.UploadAsync(token);

            if (results.Status == UploadStatus.Failed)
            {
                _logger.LogError($"Error uploading file: {results.Exception.Message}");
                throw new FailedOperationException(results.Exception.Message, results.Exception);
            }
        }

        private async Task CreateFileAsync(DriveService service, string name, Stream data, string parentId = "", CancellationToken token = default)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name,
            };

            if (!string.IsNullOrEmpty(parentId))
            {
                fileMetadata.Parents = new string[] { parentId };
            }

            var request = service.Files.Create(fileMetadata, data, "text/plain");
            request.Fields = "*";

            var results = await request.UploadAsync(token);

            if (results.Status == UploadStatus.Failed)
            {
                _logger.LogError($"Error creating file: {results.Exception.Message}");
                throw new FailedOperationException(results.Exception.Message, results.Exception);
            }
        }

        private static async Task DeleteAsync(DriveService service, string fileId, CancellationToken token = default)
        {
            var command = service.Files.Delete(fileId);
            await command.ExecuteAsync(token);
        }

        private static async Task<string> CreateFolderAsync(DriveService service, string folderName, string parentId = "", CancellationToken token = default)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
            };

            if (!string.IsNullOrEmpty(parentId))
            {
                fileMetadata.Parents = new string[] { parentId };
            }

            var request = service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = await request.ExecuteAsync(token);

            return file.Id;
        }

        private async Task ShareAsync(DriveService service, string fileId, string emailAddress, CancellationToken token = default)
        {
            Permission userPermission = new Permission()
            {
                Type = "user",
                Role = "writer",
                EmailAddress = emailAddress,
            };

            try
            {
                var request = service.Permissions.Create(userPermission, fileId);
                request.SendNotificationEmail = false;
                request.SupportsAllDrives = true;
                var file = await request.ExecuteAsync(token);
            }
            catch (Exception e)
            {
                _logger.LogError("An error occurred: " + e.Message);
            }
        }

        private static async Task<IEnumerable<Google.Apis.Drive.v3.Data.File>> GetResourcesAsync(DriveService service, string query, CancellationToken token = default)
        {
            var fileList = service.Files.List();
            fileList.Q = query;
            fileList.Fields = "nextPageToken, files(*)";

            var result = new List<Google.Apis.Drive.v3.Data.File>();
            string pageToken = null;
            do
            {
                fileList.PageToken = pageToken;
                var filesResult = await fileList.ExecuteAsync(token);
                var files = filesResult.Files;
                pageToken = filesResult.NextPageToken;
                result.AddRange(files);
            } while (pageToken != null);

            return result;
        }
    }
}
