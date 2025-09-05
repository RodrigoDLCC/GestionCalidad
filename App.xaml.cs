using GestionCalidad.Services;
using GestionCalidad.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace GestionCalidad
{
    public partial class App : Application
    {
        private AuthService _authService;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _authService = new AuthService();
                AppAuth.Initialize(_authService);

                // Verificar si ya hay una sesión activa
                bool sesionActiva = await _authService.VerificarSesionActivaAsync();

                if (sesionActiva && _authService.EstaAutenticado())
                {
                    // Ir directamente al dashboard principal
                    var dashboard = new DashboardPrincipal(
                        _authService.UsuarioActual.Id,
                        _authService.UsuarioActual.NombreCompleto
                    );
                    dashboard.Show();
                }
                else
                {
                    // Mostrar login
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Fallback: mostrar login de todos modos
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Cerrar sesión al salir de la aplicación
            _authService?.Logout();
        }
    }
}