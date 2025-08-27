using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GestionCalidad.Services
{
    public class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string ApplicationName = "GestionCalidadApp";

        private DriveService _service;
        private bool _initialized = false;
        private SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                UserCredential credential;
                string credPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "token.json");

                using (var stream = new FileStream(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestiongeca-credentials.json"),
                    FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                _service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<(string WebLink, string FileId)> SubirArchivoAsync(string filePath, string folderId)
        {
            await EnsureInitializedAsync();

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                Parents = new List<string> { folderId }
            };

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var request = _service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id, webViewLink";
                    var result = await request.UploadAsync();

                    if (result.Status == UploadStatus.Completed)
                    {
                        var file = request.ResponseBody;
                        return (file.WebViewLink, file.Id);
                    }
                    else
                    {
                        throw new Exception($"Upload error: {result.Exception?.Message ?? "Estado inesperado"}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al subir archivo a Drive: {ex.Message}", ex);
            }
        }


        public async Task HacerArchivoPublicoAsync(string fileId)
        {
            await EnsureInitializedAsync();

            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "anyone",
                Role = "reader"
            };

            await _service.Permissions.Create(permission, fileId).ExecuteAsync();
        }


        public async Task<MemoryStream> DescargarArchivoAsync(string fileId)
        {
            await EnsureInitializedAsync();

            var request = _service.Files.Get(fileId);
            var stream = new MemoryStream();

            await request.DownloadAsync(stream);
            stream.Position = 0; // Reset stream position for reading

            return stream;
        }

        public async Task<bool> EliminarArchivoAsync(string fileId)
        {
            await EnsureInitializedAsync();

            try
            {
                await _service.Files.Delete(fileId).ExecuteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListarArchivosAsync(string folderId = null)
        {
            await EnsureInitializedAsync();

            var request = _service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, size, createdTime, modifiedTime, webViewLink)";

            var result = await request.ExecuteAsync();
            return result.Files?.ToList() ?? new List<Google.Apis.Drive.v3.Data.File>();
        }

        public async Task<Google.Apis.Drive.v3.Data.File> ObtenerArchivoAsync(string fileId)
        {
            await EnsureInitializedAsync();

            var request = _service.Files.Get(fileId);
            request.Fields = "id, name, mimeType, size, createdTime, modifiedTime, webViewLink";

            return await request.ExecuteAsync();
        }
    }
}