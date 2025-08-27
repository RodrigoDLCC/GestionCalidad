using GestionCalidad.Models;
using GestionCalidad.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    public partial class RegistroDocumento : Window
    {
        private readonly MongoService _mongoService;
        private readonly GoogleDriveService _googleDriveService;
        private readonly Entidad _entidadFiltro;
        private readonly string _usuarioId;
        private readonly string _usuarioNombre;
        private List<EntidadCheckbox> _entidadesDisponibles;
        private string _archivoSeleccionado;

        public bool DocumentoRegistrado { get; private set; }

        public RegistroDocumento(Entidad entidadFiltro, string usuarioId, string usuarioNombre)
        {
            InitializeComponent();

            _entidadFiltro = entidadFiltro;
            _usuarioId = usuarioId;
            _usuarioNombre = usuarioNombre;
            _mongoService = new MongoService();
            _googleDriveService = new GoogleDriveService();

            Loaded += RegistroDocumento_Loaded;
        }

        private async void RegistroDocumento_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await CargarEntidadesAsync();

                // Si se viene de una entidad específica, preseleccionarla
                if (_entidadFiltro != null)
                {
                    var entidadPreseleccionada = _entidadesDisponibles
                        .FirstOrDefault(e => e.Entidad.Id == _entidadFiltro.Id);

                    if (entidadPreseleccionada != null)
                    {
                        entidadPreseleccionada.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar los datos: {ex.Message}");
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

        private void BtnExaminar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar documento",
                Filter = "Todos los archivos (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word documents (*.docx)|*.docx|Excel files (*.xlsx)|*.xlsx",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _archivoSeleccionado = openFileDialog.FileName;
                TxtArchivo.Text = System.IO.Path.GetFileName(_archivoSeleccionado);
            }
        }

        private async void BtnSubirDocumento_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                UploadOverlay.Visibility = Visibility.Visible;
                IsEnabled = false;

                // 1. Subir archivo a Google Drive
                var entidadesSeleccionadas = _entidadesDisponibles
                    .Where(ec => ec.IsSelected)
                    .Select(ec => ec.Entidad)
                    .ToList();

                if (!entidadesSeleccionadas.Any())
                {
                    throw new Exception("Debe seleccionar al menos una entidad");
                }

                // Usar la primera carpeta de entidad como carpeta principal
                var folderId = entidadesSeleccionadas.First().GoogleDriveFolderId;

                var (webLink, fileId) = await _googleDriveService.SubirArchivoAsync(_archivoSeleccionado, folderId);
                await _googleDriveService.HacerArchivoPublicoAsync(fileId);

                // 2. Crear documento en MongoDB
                var documento = new Documento
                {
                    Nombre = TxtNombre.Text.Trim(),
                    Codigo = TxtCodigo.Text.Trim(),
                    Tipo = (CmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Descripcion = TxtDescripcion.Text.Trim(),
                    FechaDocumento = DpFechaDocumento.SelectedDate ?? DateTime.Now,
                    FechaSubida = DateTime.Now,
                    FechaUltimaModificacion = DateTime.Now,
                    AreaDependencia = TxtAreaDependencia.Text.Trim(),
                    Estado = (CmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    UsuarioId = _usuarioId,
                    UsuarioNombre = _usuarioNombre,
                    EnlaceDrive = webLink,
                    DriveFileId = fileId,
                    VersionActual = TxtVersion.Text.Trim(),
                    EntidadesIds = entidadesSeleccionadas.Select(ent => ent.Id).ToList()
                };

                // 3. Crear versión inicial
                var versionInicial = new VersionDocumento
                {
                    NumeroVersion = TxtVersion.Text.Trim(),
                    DriveFileId = fileId,
                    Cambios = TxtCambios.Text.Trim(),
                    FechaCreacion = DateTime.Now,
                    UsuarioId = _usuarioId,
                    UsuarioNombre = _usuarioNombre,
                    Comentarios = "Versión inicial",
                    EsVersionFinal = false
                };

                documento.HistorialVersiones.Add(versionInicial);

                // 4. Guardar en MongoDB
                await _mongoService.CrearDocumentoAsync(documento);

                DocumentoRegistrado = true;

                MessageBox.Show("Documento registrado exitosamente!", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al registrar el documento: {ex.Message}");
            }
            finally
            {
                UploadOverlay.Visibility = Visibility.Collapsed;
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

            if (string.IsNullOrWhiteSpace(_archivoSeleccionado) || !File.Exists(_archivoSeleccionado))
                errores.Add("Debe seleccionar un archivo válido");

            if (string.IsNullOrWhiteSpace(TxtVersion.Text))
                errores.Add("La versión es obligatoria");

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

    // Clase auxiliar para el binding de entidades con checkbox
    public class EntidadCheckbox
    {
        public Entidad Entidad { get; set; }
        public bool IsSelected { get; set; }
    }
}