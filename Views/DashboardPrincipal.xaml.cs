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
        private string _usuarioActual;
        private string _usuarioId;
        private MongoService _mongoService;
        private List<Entidad> _entidades;

        public DashboardPrincipal(string usuarioId, string nombreUsuario)
        {
            InitializeComponent();
            _usuarioId = usuarioId;
            _usuarioActual = nombreUsuario;
            txtUsuario.Text = $"Usuario: {_usuarioActual}";

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
                //PRUEBAS
                //MessageBox.Show($"Total entidades: {_entidades.Count}");
                //foreach (var e in _entidades)
                //{
                //    Console.WriteLine($"{e.Id} - {e.Nombre}");
                //}
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
            var listaDocumentos = new ListaDocumentos(entidadSeleccionada, _usuarioId, _usuarioActual);
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