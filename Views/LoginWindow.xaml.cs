using GestionCalidad.Services;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionCalidad.Views
{
    public partial class LoginWindow : Window
    {
        private AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private async void BtnGoogleLogin_Click(object sender, RoutedEventArgs e)
        {
            await IniciarSesion(false);
        }

        private async void BtnTestLogin_Click(object sender, RoutedEventArgs e)
        {
            await IniciarSesion(true);
        }

        private async Task IniciarSesion(bool modoPrueba)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                IsEnabled = false;

                bool loginExitoso;

                if (modoPrueba)
                {
                    // Modo prueba - usuario simulado
                    loginExitoso = await LoginModoPrueba();
                }
                else
                {
                    // Modo producción - autenticación real con Google
                    loginExitoso = await _authService.LoginWithGoogleAsync();
                }

                if (loginExitoso)
                {
                    // Guardar el servicio de autenticación para uso global
                    AppAuth.AuthService = _authService;

                    AbrirDashboardPrincipal();
                }
                else
                {
                    MessageBox.Show("Error en el inicio de sesión. Por favor, intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                IsEnabled = true;
            }
        }

        private async Task<bool> LoginModoPrueba()
        {
            try
            {
                // login testing
                var usuarioPrueba = new Models.Usuario
                {
                    GoogleId = "test_123456",
                    Email = "rodrigo@upt.edu.pe",
                    Nombre = "Rodrigo",
                    Apellido = "Choque",
                    NombreCompleto = "Rodrigo Choque",
                    FotoUrl = "",
                    Rol = "Administrador"
                };

                return await _authService.LoginWithTestUserAsync(usuarioPrueba);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en modo prueba: {ex.Message}");
                return false;
            }
        }

        private void AbrirDashboardPrincipal()
        {
            if (_authService?.UsuarioActual == null)
            {
                MessageBox.Show("Error: No hay usuario autenticado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dashboard = new DashboardPrincipal(
                _authService.UsuarioActual.Id,
                _authService.UsuarioActual.NombreCompleto
            );

            dashboard.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}