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
    /// <summary>
    /// Lógica de interacción para NuevaVersionDocumento.xaml
    /// </summary>
    public partial class NuevaVersionDocumento : Window
    {
        private readonly Documento _documento;
        private readonly MongoService _mongoService;
        private readonly GoogleDriveService _googleDriveService;
        private string _archivoSeleccionado;

        public NuevaVersionDocumento(Documento documento, MongoService mongoService)
        {
            InitializeComponent();

            _documento = documento ?? throw new ArgumentNullException(nameof(documento));
            _mongoService = mongoService ?? new MongoService();
            _googleDriveService = new GoogleDriveService();

            Loaded += NuevaVersionWindow_Loaded;
        }

        private void NuevaVersionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtDocumento.Text = $"{_documento.Nombre} (v{_documento.VersionActual})";
            TxtNuevaVersion.Text = ObtenerSiguienteVersion(_documento.VersionActual);

            // Pre-llenar el campo de cambios con una sugerencia
            TxtCambios.Text = "Actualizaciones del documento.";
            TxtCambios.SelectAll(); // Seleccionar todo el texto para fácil edición
            TxtCambios.Focus();     // Poner foco en este campo
        }

        private string ObtenerSiguienteVersion(string versionActual)
        {
            // Lógica simple para incrementar versión: 1.0 -> 1.1, 1.1 -> 1.2, etc.
            if (decimal.TryParse(versionActual, out decimal version))
            {
                return (version + 0.1m).ToString("0.0");
            }

            // Si no se puede parsear, usar 1.0 como versión inicial
            return "1.0";
        }

        private void BtnExaminar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar nueva versión del documento",
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

        private async void BtnSubirVersion_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            try
            {
                UploadOverlay.Visibility = Visibility.Visible;
                IsEnabled = false;

                // 1. Subir nueva versión a Google Drive (misma carpeta)
                var folderId = await ObtenerFolderIdEntidad();
                var (webLink, fileId) = await _googleDriveService.SubirArchivoAsync(_archivoSeleccionado, folderId);
                await _googleDriveService.HacerArchivoPublicoAsync(fileId);

                // 2. Crear nueva versión del documento
                var nuevaVersion = new VersionDocumento
                {
                    NumeroVersion = TxtNuevaVersion.Text.Trim(),
                    DriveFileId = fileId,
                    Cambios = TxtCambios.Text.Trim(),
                    FechaCreacion = DateTime.Now,
                    UsuarioId = _documento.UsuarioId,
                    UsuarioNombre = _documento.UsuarioNombre,
                    Comentarios = "Nueva versión registrada",
                    EsVersionFinal = false
                };

                // 3. Actualizar documento en MongoDB
                await _mongoService.AgregarVersionADocumentoAsync(_documento.Id, nuevaVersion);

                MessageBox.Show("Nueva versión registrada exitosamente!", "Éxito",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al registrar la nueva versión: {ex.Message}");
            }
            finally
            {
                UploadOverlay.Visibility = Visibility.Collapsed;
                IsEnabled = true;
            }
        }

        private async Task<string> ObtenerFolderIdEntidad()
        {
            if (_documento.EntidadesIds != null && _documento.EntidadesIds.Any())
            {
                var primeraEntidad = await _mongoService.ObtenerEntidadPorIdAsync(_documento.EntidadesIds[0]);
                return primeraEntidad?.GoogleDriveFolderId;
            }
            throw new Exception("No se pudo determinar la carpeta de destino");
        }

        private bool ValidarFormulario()
        {
            TxtMensajeError.Visibility = Visibility.Collapsed;
            var errores = new List<string>();

            // Validar versión
            if (string.IsNullOrWhiteSpace(TxtNuevaVersion.Text))
                errores.Add("• La nueva versión es obligatoria");
            else if (!EsVersionValida(TxtNuevaVersion.Text))
                errores.Add("• El formato de versión no es válido (ej: 1.0, 2.1, etc.)");

            // Validar cambios
            if (string.IsNullOrWhiteSpace(TxtCambios.Text))
                errores.Add("• Los cambios en esta versión son obligatorios");
            else if (TxtCambios.Text.Trim().Length < 10)
                errores.Add("• Por favor describa los cambios con más detalle (mínimo 10 caracteres)");

            // Validar archivo
            if (string.IsNullOrWhiteSpace(_archivoSeleccionado))
                errores.Add("• Debe seleccionar un archivo");
            else if (!File.Exists(_archivoSeleccionado))
                errores.Add("• El archivo seleccionado no existe o no es accesible");
            else if (new FileInfo(_archivoSeleccionado).Length == 0)
                errores.Add("• El archivo seleccionado está vacío");

            if (errores.Any())
            {
                MostrarError("Se encontraron los siguientes errores:\n\n" + string.Join("\n", errores));
                return false;
            }

            return true;
        }

        // Método auxiliar para validar formato de versión
        private bool EsVersionValida(string version)
        {
            // Validar formato como 1, 1.0, 2.5, etc.
            return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+(\.\d+)?$");
        }


        private void MostrarError(string mensaje)
        {
            // Hacer visible el TextBlock de error
            TxtMensajeError.Visibility = Visibility.Visible;
            TxtMensajeError.Text = mensaje;

            // También mostrar en MessageBox para debugging
            MessageBox.Show(mensaje, "Error de validación", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}