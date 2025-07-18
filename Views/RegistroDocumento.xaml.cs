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
    /// Lógica de interacción para RegistroDocumento.xaml
    /// </summary>
    public partial class RegistroDocumento : Window
    {
        private readonly MongoService _mongoService;
        private readonly GoogleDriveService _driveService;
        private const string DriveFolderId = "14p_335OMbuBQ4KEhlkXdz4Rt6x6o17zA"; // ID de carpeta en Drive

        public RegistroDocumento()
        {
            InitializeComponent();
            _mongoService = new MongoService();
            _driveService = new GoogleDriveService();
            dpFechaDocumento.SelectedDate = DateTime.Today; // Establecer fecha actual por defecto
        }

        private void BtnSeleccionarArchivo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Todos los archivos|*.*",
                Title = "Seleccionar archivo para subir",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtRutaArchivo.Text = openFileDialog.FileName;
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos())
                return;

            try
            {
                var (enlaceDrive, driveFileId) = await SubirArchivoYConfigurarPermisos();
                var documento = CrearDocumento(enlaceDrive, driveFileId);

                GuardarDocumentoEnBaseDeDatos(documento);

                MostrarMensajeExito();
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al guardar el documento: {ex.Message}");
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarError("El nombre del documento es obligatorio.");
                return false;
            }

            if (dpFechaDocumento.SelectedDate == null)
            {
                MostrarError("Seleccione una fecha válida.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtRutaArchivo.Text) || !File.Exists(txtRutaArchivo.Text))
            {
                MostrarError("Debe seleccionar un archivo válido.");
                return false;
            }

            return true;
        }

        private async Task<(string enlaceDrive, string driveFileId)> SubirArchivoYConfigurarPermisos()
        {
            await _driveService.InicializarAsync();
            var (webLink, fileId) = await _driveService.SubirArchivoAsync(txtRutaArchivo.Text, DriveFolderId);
            await _driveService.HacerArchivoPublicoAsync(fileId);
            return (webLink, fileId);
        }

        private Documento CrearDocumento(string enlaceDrive, string driveFileId)
        {
            var entidadesSeleccionadas = ObtenerEntidadesSeleccionadas();

            if (entidadesSeleccionadas == null || entidadesSeleccionadas.Count == 0)
            {
                entidadesSeleccionadas = new List<string> { "General" };
            }

            return new Documento
            {
                Nombre = string.IsNullOrWhiteSpace(txtNombre.Text) ? "Sin nombre" : txtNombre.Text,
                Entidades = entidadesSeleccionadas,
                Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Otros",
                FechaDocumento = dpFechaDocumento.SelectedDate ?? DateTime.Today,
                Version = string.IsNullOrWhiteSpace(txtVersion.Text) ? "1.0" : txtVersion.Text,
                AreaDependencia = string.IsNullOrWhiteSpace(txtAreaDependencia.Text) ? "No especificado" : txtAreaDependencia.Text,
                Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? "Sin descripción" : txtDescripcion.Text,
                Usuario = txtUsuario.Text,
                FechaSubida = DateTime.Now,
                EnlaceDrive = enlaceDrive ?? "",
                DriveFileId = driveFileId ?? "",
                Estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Vigente",
                Codigo = txtCodigo.Text
            };
        }

        private List<string> ObtenerEntidadesSeleccionadas()
        {
            var entidades = new List<string>();
            if (chkSUNEDU.IsChecked == true) entidades.Add("SUNEDU");
            if (chkSINEACE.IsChecked == true) entidades.Add("SINEACE");
            if (chkICACIT.IsChecked == true) entidades.Add("ICACIT");
            if (chkISO9001.IsChecked == true) entidades.Add("ISO 9001");
            if (chkISO21001.IsChecked == true) entidades.Add("ISO 21001");
            return entidades;
        }

        private void GuardarDocumentoEnBaseDeDatos(Documento documento)
        {
            _mongoService.Documentos.InsertOne(documento);
        }

        private void MostrarMensajeExito()
        {
            MessageBox.Show("Documento guardado correctamente con archivo en Google Drive.",
                          "Éxito",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void MostrarError(string mensaje)
        {
            MessageBox.Show(mensaje,
                          "Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
