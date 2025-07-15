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

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
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

        public async Task<string> SubirArchivoAsync(string filePath)
        {
            if (_service == null)
                await InicializarAsync();

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath)
            };

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var request = _service.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id, webViewLink";
                var result = await request.UploadAsync();

                if (result.Status == UploadStatus.Completed)
                {
                    var file = request.ResponseBody;
                    return file.WebViewLink; // También puedes guardar file.Id si lo necesitas
                }
                else
                {
                    throw new Exception("Error al subir archivo a Drive");
                }
            }
        }
    }
}
