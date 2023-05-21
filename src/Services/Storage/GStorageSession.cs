using Google.Apis.Drive.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using Google.Apis.Download;
using System.Data;

namespace Telegram.Bot.Examples.WebHook.Services.Storage
{
    public class GStorageSession : IGStorageSession
    {
        private readonly DriveService _driveService;
        private readonly GStorageConfiguration _options;
        private readonly ILogger _logger;

        private readonly string _folderId;

        public GStorageSession(DriveService driveService, string folderId, GStorageConfiguration options, ILogger logger)
        {
            _driveService = driveService;
            _folderId = folderId;
            _logger = logger;
            _options = options;
        }

        public static async Task<string> InitAsync(DriveService driveService, GStorageConfiguration options, CancellationToken token = default)
        {
            var folders = await GetResourcesAsync(driveService, "mimeType='application/vnd.google-apps.folder' and 'root' in parents", false, token);
            var storageFolder = folders.Where(folder => folder.Name == options.StorageName).FirstOrDefault();
            if (storageFolder != null)
            {
                return storageFolder.Id;
            }
            else
            {
                var folderId = await CreateFolderAsync(driveService, options.StorageName, token: token);
                if (!string.IsNullOrEmpty(options.ShareTo))
                {
                    await ShareAsync(driveService, folderId, options.ShareTo, token);
                }

                return folderId;
            }
        }

        public async Task CreateFileAsync(string name, Stream data, StorageEntryOptions options, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (data == null)
                throw new ArgumentException(nameof(data));

            var properties = options.ToProperties(); 
            await CreateFileAsync(_driveService, name, data, properties, token, _folderId);
        }

        public async Task UpdateFileAsync(string name, Stream data, StorageEntryOptions options, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (data == null)
                throw new ArgumentException(nameof(data));

            var filses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", false, token);
            var file = filses.Where(f => f.Name == name).FirstOrDefault();

            if (file == null)
                throw new NotFoundException();

            var properties = options.ToProperties();
            await UpdateFileAsync(_driveService, file.Id, data, properties, token);
        }

        public async Task DownloadFileAsync(string name, Stream data, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            var filses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", false, token);
            var file = filses.Where(f => f.Name == name).FirstOrDefault();

            if (file == null)
                throw new NotFoundException();

            await DownloadFileAsync(_driveService, file.Id, data, token);
        }
        public async Task DeleteAsync(string name, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            var filses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", false, token);
            var file = filses.Where(f => f.Name == name).FirstOrDefault();

            if (file == null)
                throw new NotFoundException();

            await DeleteAsync(_driveService, file.Id, token);
        }

        public async Task<IEnumerable<StorageEntryOptions>> FindStorageOptionsAsync(string name, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            var filses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", false, token);
            var storageOptions = filses.Where(f => f.Name == name)
                .Select(f => StorageEntryOptionsExtensions.MakeStorageEntryOptions(f.Properties))
                .ToList();

            return storageOptions;
        }

        public void Dispose()
        {
            try
            {
                _driveService?.Dispose();
            }
            catch
            {
                _logger.LogWarning($"Failed disposing drive service {this._folderId}");
            }
        }

        public async Task<IEnumerable<string>> GetFileIdsAsync(CancellationToken token = default)
        {
            var filses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", false, token);
            return filses.Select(f => f.Id).ToList();
        }
        public async Task CleanupStorageAsync(CancellationToken token = default)
        {
            var expiredFilses = await GetResourcesAsync(_driveService, $"mimeType!='application/vnd.google-apps.folder' and '{_folderId}' in parents", true, token);
            foreach (var expiredFilse in expiredFilses)
            {
                await DeleteAsync(_driveService, expiredFilse.Id, token);
            }
        }

        private async Task DownloadFileAsync(DriveService service, string fileId, Stream stream, CancellationToken token)
        {
            var request = service.Files.Get(fileId);
            var results = await request.DownloadAsync(stream, token);

            if (results.Status == DownloadStatus.Failed)
            {
                _logger.LogError($"Error downloing file: {results.Exception.Message}");
                throw new FailedOperationException(results.Exception.Message, results.Exception);
            }
        }

        private async Task UpdateFileAsync(DriveService service, string id, Stream data, IDictionary<string, string> properties, CancellationToken token)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File();

            fileMetadata.Properties = properties;

            var request = service.Files.Update(fileMetadata, id, data, "text/plain");
            request.Fields = "*";

            var results = await request.UploadAsync(token);

            if (results.Status == UploadStatus.Failed)
            {
                _logger.LogError($"Error uploading file: {results.Exception.Message}");
                throw new FailedOperationException(results.Exception.Message, results.Exception);
            }
        }

        private async Task CreateFileAsync(DriveService service, string name, Stream data, IDictionary<string, string> properties, CancellationToken token, string parentId = "")
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name,
                Properties = properties,
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

        private static async Task DeleteAsync(DriveService service, string fileId, CancellationToken token)
        {
            var command = service.Files.Delete(fileId);
            await command.ExecuteAsync(token);
        }

        private static async Task<string> CreateFolderAsync(DriveService service, string folderName, CancellationToken token, string parentId = "")
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

        private static async Task ShareAsync(DriveService service, string fileId, string emailAddress, CancellationToken token)
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
            catch (Exception)
            {
            }
        }

        private static async Task<IEnumerable<Google.Apis.Drive.v3.Data.File>> GetResourcesAsync(DriveService service, string query, bool expired, CancellationToken token)
        {
            var fileList = service.Files.List();
            fileList.Q = query;
            fileList.Fields = "nextPageToken, files(*)";

            var result = new List<Google.Apis.Drive.v3.Data.File>();
            string pageToken = string.Empty;
            do
            {
                fileList.PageToken = pageToken;
                var filesResult = await fileList.ExecuteAsync(token);
                var files = filesResult.Files
                    .Where(f => expired == StorageEntryOptionsExtensions.IsExpired(f.ModifiedTime, f.Properties))
                    .ToList();


                pageToken = filesResult.NextPageToken;
                result.AddRange(files);
            } while (pageToken != null);

            return result;
        }
    }
}
