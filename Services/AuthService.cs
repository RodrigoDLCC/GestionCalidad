using GestionCalidad.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GestionCalidad.Services
{
    public class AuthService
    {
        private readonly MongoService _mongoService;

        // Credenciales desde tu archivo JSON
        private static readonly string[] Scopes = {
            "https://www.googleapis.com/auth/userinfo.email",
            "https://www.googleapis.com/auth/userinfo.profile"
        };

        private static readonly string ApplicationName = "GestionCalidadApp";

        public Usuario UsuarioActual { get; private set; }

        public AuthService()
        {
            _mongoService = new MongoService();
            VerificarCredenciales();
        }

        private void VerificarCredenciales()
        {
            try
            {
                string credencialesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestiongeca-credentials.json");

                if (!File.Exists(credencialesPath))
                {
                    MessageBox.Show("Archivo de credenciales de Google no encontrado. La autenticación no funcionará.",
                                  "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando credenciales: {ex.Message}");
            }
        }

        public async Task<bool> LoginWithGoogleAsync()
        {
            try
            {
                UserCredential credential;

                string credencialesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestiongeca-credentials.json");
                if (!File.Exists(credencialesPath))
                {
                    MessageBox.Show("Archivo de credenciales no encontrado. Usando modo prueba.");
                    return await LoginModoPruebaAutomatico();
                }

                using (var stream = new FileStream(credencialesPath, FileMode.Open, FileAccess.Read))
                {
                    string credPath = "google_token.json";
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                // Obtener información del usuario
                var oauthService = new Oauth2Service(
                    new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });

                var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();

                // Validar que se obtuvieron los datos correctamente
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Id))
                {
                    MessageBox.Show("No se pudieron obtener los datos del usuario de Google.");
                    return false;
                }

                // Crear objeto usuario con la información de Google
                var usuarioGoogle = new Usuario
                {
                    GoogleId = userInfo.Id,
                    Email = userInfo.Email,
                    Nombre = userInfo.GivenName,
                    Apellido = userInfo.FamilyName,
                    NombreCompleto = userInfo.Name,
                    FotoUrl = userInfo.Picture,
                    Rol = ObtenerRolPorEmail(userInfo.Email)
                };

                UsuarioActual = await ObtenerOCrearUsuarioAsync(usuarioGoogle);
                return UsuarioActual != null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en autenticación Google: {ex.Message}");
                // Fallback a modo prueba
                return await LoginModoPruebaAutomatico();
            }
        }

        private async Task<bool> LoginModoPruebaAutomatico()
        {
            // Fallback automático a modo prueba si la autenticación Google falla
            var testUser = new Usuario
            {
                GoogleId = "test_fallback",
                Email = "rodrigo@upt.edu.pe",
                Nombre = "Rodrigo",
                Apellido = "Choque",
                NombreCompleto = "Rodrigo Choque",
                FotoUrl = "https://example.com/photo.jpg",
                Rol = "Administrador"
            };

            return await LoginWithTestUserAsync(testUser);
        }

        // Método para login de prueba (modo testing)
        public async Task<bool> LoginWithTestUserAsync(Usuario testUser)
        {
            try
            {
                if (testUser == null)
                {
                    throw new ArgumentNullException(nameof(testUser));
                }

                UsuarioActual = await ObtenerOCrearUsuarioAsync(testUser);
                return UsuarioActual != null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en autenticación de prueba: {ex.Message}");
                return false;
            }
        }

        private string ObtenerRolPorEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "Usuario";

            // Lógica para asignar roles basado en el email
            if (email.EndsWith("@upt.edu.pe") || email.EndsWith("@admin.com"))
            {
                return "Administrador";
            }
            return "Usuario";
        }

        private async Task<Usuario> ObtenerOCrearUsuarioAsync(Usuario usuarioGoogle)
        {
            try
            {
                if (usuarioGoogle == null)
                {
                    throw new ArgumentNullException(nameof(usuarioGoogle));
                }

                if (string.IsNullOrEmpty(usuarioGoogle.GoogleId))
                {
                    throw new ArgumentException("GoogleId no puede estar vacío");
                }

                // Buscar usuario por Google ID
                var usuarioExistente = await _mongoService.Usuarios
                    .Find(u => u.GoogleId == usuarioGoogle.GoogleId)
                    .FirstOrDefaultAsync();

                if (usuarioExistente != null)
                {
                    // Actualizar información y último acceso
                    usuarioExistente.Nombre = usuarioGoogle.Nombre;
                    usuarioExistente.Apellido = usuarioGoogle.Apellido;
                    usuarioExistente.NombreCompleto = usuarioGoogle.NombreCompleto;
                    usuarioExistente.Email = usuarioGoogle.Email;
                    usuarioExistente.FotoUrl = usuarioGoogle.FotoUrl;
                    usuarioExistente.UltimoAcceso = DateTime.Now;

                    var result = await _mongoService.Usuarios.ReplaceOneAsync(
                        u => u.Id == usuarioExistente.Id,
                        usuarioExistente);

                    return result.IsAcknowledged ? usuarioExistente : null;
                }

                // Crear nuevo usuario
                usuarioGoogle.FechaRegistro = DateTime.Now;
                usuarioGoogle.UltimoAcceso = DateTime.Now;
                usuarioGoogle.Activo = true;

                await _mongoService.Usuarios.InsertOneAsync(usuarioGoogle);
                return usuarioGoogle;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener/crear usuario: {ex.Message}");
                return null;
            }
        }

        public void Logout()
        {
            // Limpiar token de Google
            try
            {
                string credPath = "google_token.json";
                if (File.Exists(credPath))
                {
                    File.Delete(credPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al limpiar token: {ex.Message}");
            }

            UsuarioActual = null;
        }

        public bool EstaAutenticado()
        {
            return UsuarioActual != null;
        }

        public bool EsAdministrador()
        {
            return EstaAutenticado() && UsuarioActual.Rol == "Administrador";
        }

        // Método para verificar si el token de Google sigue siendo válido
        public async Task<bool> VerificarSesionActivaAsync()
        {
            try
            {
                string credPath = "google_token.json";
                if (!File.Exists(credPath))
                {
                    return false;
                }

                // Verificar si tenemos un usuario en memoria
                if (UsuarioActual != null)
                {
                    return true;
                }

                // Intentar cargar las credenciales
                string credencialesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gestiongeca-credentials.json");
                if (!File.Exists(credencialesPath))
                {
                    return false;
                }

                using (var stream = new FileStream(credencialesPath, FileMode.Open, FileAccess.Read))
                {
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));

                    // Si llegamos aquí, el token es válido
                    // Ahora podemos cargar la información del usuario
                    if (credential.Token.IsStale == false)
                    {
                        var oauthService = new Oauth2Service(
                            new BaseClientService.Initializer()
                            {
                                HttpClientInitializer = credential,
                                ApplicationName = ApplicationName,
                            });

                        var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();

                        if (userInfo != null && !string.IsNullOrEmpty(userInfo.Id))
                        {
                            // Buscar el usuario en la base de datos
                            var usuario = await _mongoService.Usuarios
                                .Find(u => u.GoogleId == userInfo.Id)
                                .FirstOrDefaultAsync();

                            if (usuario != null)
                            {
                                UsuarioActual = usuario;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}