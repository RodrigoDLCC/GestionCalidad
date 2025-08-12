using GestionCalidad.Models;
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
    /// <summary>
    /// Lógica de interacción para ListaDocumentos.xaml
    /// </summary>
    public partial class ListaDocumentos : Window
    {
        private readonly MongoService _mongoService;
        private List<Documento> _documentosCompletos;
        private readonly string _entidadFiltro;
        private readonly string _usuarioActual;
        public string TituloEntidad => _entidadFiltro == "TODOS" ? "TODOS LOS DOCUMENTOS" : $"DOCUMENTOS DE {_entidadFiltro}";

        public ListaDocumentos(string entidadFiltro, string usuarioActual)
        {
            if (string.IsNullOrEmpty(entidadFiltro))
            {
                throw new ArgumentException("La entidad de filtro no puede estar vacía");
            }

            _entidadFiltro = entidadFiltro;
            _usuarioActual = usuarioActual;

            InitializeComponent();
            this.DataContext = this;
            _mongoService = new MongoService();
            CargarDocumentos();
        }

        private void CargarDocumentos()
        {
            try
            {
                var filter = _entidadFiltro == "TODOS"
                ? Builders<Documento>.Filter.Empty
                : Builders<Documento>.Filter.AnyEq(x => x.Entidades, _entidadFiltro);

                _documentosCompletos = _mongoService.Documentos.Find(filter).ToList();
                ActualizarListado(_documentosCompletos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar documentos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarListado(List<Documento> documentos) //MODIFICANDO 08 08
        {
            var items = documentos.Select(d => new DocumentoListItem
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Tipo = d.Tipo,
                Version = d.Version,
                FechaDocumento = d.FechaDocumento.ToString("dd/MM/yyyy"),
                AreaDependencia = d.AreaDependencia,
                Estado = d.Estado,
                EnlaceDrive = d.EnlaceDrive
            }).ToList();

            dgDocumentos.ItemsSource = items;
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            // Aplica primero el filtro por entidad
            var documentosFiltrados = _documentosCompletos.AsQueryable()
                .Where(d => _entidadFiltro == "TODOS" || d.Entidades.Contains(_entidadFiltro));

            // Luego aplica los filtros adicionales
            if (cmbFiltroTipo.SelectedIndex > 0)
            {
                var tipoSeleccionado = (cmbFiltroTipo.SelectedItem as ComboBoxItem)?.Content.ToString();
                documentosFiltrados = documentosFiltrados.Where(d => d.Tipo == tipoSeleccionado);
            }

            if (cmbFiltroEstado.SelectedIndex > 0)
            {
                var estadoSeleccionado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content.ToString();
                documentosFiltrados = documentosFiltrados.Where(d => d.Estado == estadoSeleccionado);
            }

            if (!string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                var busqueda = txtBusqueda.Text.ToLower();
                documentosFiltrados = documentosFiltrados.Where(d =>
                    d.Nombre.ToLower().Contains(busqueda) ||
                    (d.Descripcion != null && d.Descripcion.ToLower().Contains(busqueda)));
            }

            ActualizarListado(documentosFiltrados.ToList());
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbFiltroTipo.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndex = 0;
            txtBusqueda.Clear();

            // Vuelve a cargar solo con el filtro principal de entidad
            var filter = _entidadFiltro == "TODOS"
                ? Builders<Documento>.Filter.Empty
                : Builders<Documento>.Filter.AnyEq(x => x.Entidades, _entidadFiltro);

            _documentosCompletos = _mongoService.Documentos.Find(filter).ToList();
            ActualizarListado(_documentosCompletos);
        }

        private void BtnVerDocumento_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var documentoId = button?.Tag?.ToString();

            if (string.IsNullOrEmpty(documentoId))
            {
                MessageBox.Show("No se pudo identificar el documento seleccionado.",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return;
            }

            try
            {
                var documento = _documentosCompletos.FirstOrDefault(d => d.Id == documentoId);
                if (documento == null)
                {
                    MessageBox.Show("El documento seleccionado no existe o no se pudo cargar.",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    return;
                }

                var detalleWindow = new DetalleDocumento(documento)
                {
                    Owner = this // Establece la ventana padre
                };
                detalleWindow.ShowDialog();

                // Opcional: Actualizar lista si hubo cambios
                if (detalleWindow.DocumentoModificado)
                {
                    CargarDocumentos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el documento: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void BtnDescargarDocumento_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var enlaceDrive = button?.Tag?.ToString();

            if (!string.IsNullOrEmpty(enlaceDrive))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = enlaceDrive,
                    UseShellExecute = true
                });
            }
        }

        private void BtnNuevoDocumento_Click(object sender, RoutedEventArgs e)
        {
            var registroWindow = new RegistroDocumento(_entidadFiltro);
            registroWindow.ShowDialog();
            CargarDocumentos(); // Refrescar listado después de cerrar
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}