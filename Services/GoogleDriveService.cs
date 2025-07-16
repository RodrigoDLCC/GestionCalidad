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

namespace GestionCalidad.Services
{
    public class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string ApplicationName = "GestionCalidadApp";

        private DriveService _service;

        public async Task InicializarAsync()
        {
            UserCredential credential;

            using (var stream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestiongeca-credentials.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
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
        }

        public async Task<(string WebLink, string FileId)> SubirArchivoAsync(string filePath, string folderId)
        {
            if (_service == null)
                await InicializarAsync();

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
            if (_service == null)
                await InicializarAsync();

            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Type = "anyone",
                Role = "reader"
            };

            await _service.Permissions.Create(permission, fileId).ExecuteAsync();
        }
    }
}
