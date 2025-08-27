using GestionCalidad.Models;
using GestionCalidad.Services;
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
    /// Lógica de interacción para EditarDocumento.xaml
    /// </summary>
    public partial class EditarDocumento : Window
    {
        private readonly Documento _documento;
        private readonly MongoService _mongoService;
        private List<EntidadCheckbox> _entidadesDisponibles;
        private string _nuevoArchivoSeleccionado;

        public bool DocumentoModificado { get; private set; }

        public EditarDocumento(Documento documento, MongoService mongoService = null)
        {
            InitializeComponent();

            _documento = documento ?? throw new ArgumentNullException(nameof(documento));
            _mongoService = mongoService ?? new MongoService();

            Loaded += EditarDocumento_Loaded;
        }

        private async void EditarDocumento_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                await CargarDatosDocumentoAsync();
                await CargarEntidadesAsync();
                CargarFormulario();
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                MostrarError($"Error al cargar los datos: {ex.Message}");
            }
        }

        private void CargarFormulario()
        {
            TxtNombre.Text = _documento.Nombre;
            TxtCodigo.Text = _documento.Codigo;
            TxtDescripcion.Text = _documento.Descripcion;
            DpFechaDocumento.SelectedDate = _documento.FechaDocumento;
            TxtAreaDependencia.Text = _documento.AreaDependencia;
            TxtArchivoActual.Text = _documento.Nombre;

            // Seleccionar tipo
            foreach (ComboBoxItem item in CmbTipo.Items)
            {
                if (item.Content.ToString() == _documento.Tipo)
                {
                    CmbTipo.SelectedItem = item;
                    break;
                }
            }

            // Seleccionar estado
            foreach (ComboBoxItem item in CmbEstado.Items)
            {
                if (item.Content.ToString() == _documento.Estado)
                {
                    CmbEstado.SelectedItem = item;
                    break;
                }
            }

            // Preseleccionar entidades
            if (_documento.EntidadesIds != null && _entidadesDisponibles != null)
            {
                foreach (var entidadCheckbox in _entidadesDisponibles)
                {
                    entidadCheckbox.IsSelected = _documento.EntidadesIds.Contains(entidadCheckbox.Entidad.Id);
                }
            }
        }

        private async System.Threading.Tasks.Task CargarDatosDocumentoAsync()
        {
            // Recargar el documento para tener los datos más actualizados
            var documentoActualizado = await _mongoService.ObtenerDocumentoPorIdAsync(_documento.Id);
            if (documentoActualizado != null)
            {
                _documento.Nombre = documentoActualizado.Nombre;
                _documento.Codigo = documentoActualizado.Codigo;
                _documento.Tipo = documentoActualizado.Tipo;
                _documento.Descripcion = documentoActualizado.Descripcion;
                _documento.FechaDocumento = documentoActualizado.FechaDocumento;
                _documento.AreaDependencia = documentoActualizado.AreaDependencia;
                _documento.Estado = documentoActualizado.Estado;
                _documento.EntidadesIds = documentoActualizado.EntidadesIds;
                _documento.VersionActual = documentoActualizado.VersionActual;
                _documento.HistorialVersiones = documentoActualizado.HistorialVersiones;
            }
        }

        private async System.Threading.Tasks.Task CargarEntidadesAsync()
        {
            try
            {
                var entidades = await _mongoService.ObtenerTodasEntidadesAsync();
                _entidadesDisponibles = entidades
                    .Where(e => e.Activo)
                    .Select(e => new EntidadCheckbox { Entidad = e, IsSelected = false })
                    .ToList();

                LstEntidadesDisponibles.ItemsSource = _entidadesDisponibles;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar las entidades: " + ex.Message, ex);
            }
        }

        private void BtnVerEnDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_documento.EnlaceDrive))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _documento.EnlaceDrive,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("No hay enlace disponible para este documento.",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir en Google Drive: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                IsEnabled = false;

                // Obtener entidades seleccionadas
                var entidadesSeleccionadas = _entidadesDisponibles
                    .Where(ec => ec.IsSelected)
                    .Select(ec => ec.Entidad)
                    .ToList();

                if (!entidadesSeleccionadas.Any())
                {
                    throw new Exception("Debe seleccionar al menos una entidad");
                }

                // Actualizar documento
                _documento.Nombre = TxtNombre.Text.Trim();
                _documento.Codigo = TxtCodigo.Text.Trim();
                _documento.Tipo = (CmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString();
                _documento.Descripcion = TxtDescripcion.Text.Trim();
                _documento.FechaDocumento = DpFechaDocumento.SelectedDate ?? DateTime.Now;
                _documento.AreaDependencia = TxtAreaDependencia.Text.Trim();
                _documento.Estado = (CmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString();
                _documento.EntidadesIds = entidadesSeleccionadas.Select(ent => ent.Id).ToList();
                _documento.FechaUltimaModificacion = DateTime.Now;

                // Guardar cambios en MongoDB
                var resultado = await _mongoService.ActualizarDocumentoAsync(_documento.Id, _documento);

                if (resultado)
                {
                    DocumentoModificado = true;
                    MessageBox.Show("Documento actualizado exitosamente!", "Éxito",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    throw new Exception("No se pudo actualizar el documento en la base de datos");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al actualizar el documento: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                IsEnabled = true;
            }
        }

        private bool ValidarFormulario()
        {
            TxtMensajeError.Visibility = Visibility.Collapsed;
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
                errores.Add("El nombre del documento es obligatorio");

            if (string.IsNullOrWhiteSpace(TxtCodigo.Text))
                errores.Add("El código del documento es obligatorio");

            if (CmbTipo.SelectedItem == null)
                errores.Add("Debe seleccionar un tipo de documento");

            if (DpFechaDocumento.SelectedDate == null)
                errores.Add("La fecha del documento es obligatoria");

            if (string.IsNullOrWhiteSpace(TxtAreaDependencia.Text))
                errores.Add("El área/dependencia es obligatoria");

            if (CmbEstado.SelectedItem == null)
                errores.Add("Debe seleccionar un estado");

            var entidadesSeleccionadas = _entidadesDisponibles?.Count(ec => ec.IsSelected) ?? 0;
            if (entidadesSeleccionadas == 0)
                errores.Add("Debe seleccionar al menos una entidad");

            if (errores.Any())
            {
                MostrarError(string.Join("\n", errores));
                return false;
            }

            return true;
        }

        private void MostrarError(string mensaje)
        {
            TxtMensajeError.Text = mensaje;
            TxtMensajeError.Visibility = Visibility.Visible;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Si se cierra con X, establecer DialogResult
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
            }
        }
    }
}