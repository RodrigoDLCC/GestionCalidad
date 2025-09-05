using GestionCalidad.Models;
using GestionCalidad.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GestionCalidad.Views
{
    public partial class DashboardPrincipal : Window
    {
        private string _usuarioNombre;
        private string _usuarioId;
        private MongoService _mongoService;
        private List<Entidad> _entidades;

        public DashboardPrincipal(string usuarioId, string usuarioNombre)
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(usuarioId) || string.IsNullOrEmpty(usuarioNombre))
            {
                MessageBox.Show("Error: Credenciales de usuario inválidas.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            _usuarioId = usuarioId;
            _usuarioNombre = usuarioNombre;
            txtUsuario.Text = $"Usuario: {_usuarioNombre}";

            // Inicializar servicios
            _mongoService = new MongoService();

            // Cargar entidades
            LoadEntitiesAsync();
        }

        private async void LoadEntitiesAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                // Usar dispatcher para operaciones async en hilo UI
                await Dispatcher.BeginInvoke(new Action(async () =>
                {
                    _entidades = await _mongoService.ObtenerTodasEntidadesAsync();

                    // Filtrar solo entidades activas
                    var entidadesActivas = _entidades.FindAll(e => e.Activo);

                    // Asignar al ItemsControl
                    EntitiesItemsControl.ItemsSource = entidadesActivas;

                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar las entidades: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EntityButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string entidadId = button.Tag.ToString();
            Entidad entidadSeleccionada = null;

            if (entidadId != "TODOS")
            {
                // Buscar la entidad seleccionada
                entidadSeleccionada = _entidades?.Find(ent => ent.Id == entidadId);
                if (entidadSeleccionada == null)
                {
                    MessageBox.Show("Entidad no encontrada", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Abrir ventana de lista de documentos
            var listaDocumentos = new ListaDocumentos(entidadSeleccionada, _usuarioId, _usuarioNombre);
            listaDocumentos.Show();
            this.Hide();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}