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

        public RegistroDocumento()
        {
            InitializeComponent();
            _mongoService = new MongoService();  // usar tu servicio de Mongo
            _driveService = new GoogleDriveService();
        }

        private void BtnSeleccionarArchivo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Todos los archivos|*.*",
                Title = "Seleccionar archivo para subir"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtRutaArchivo.Text = openFileDialog.FileName;
            }
        }


        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (dpFechaDocumento.SelectedDate == null)
            {
                MessageBox.Show("Seleccione una fecha válida.");
                return;
            }

            DateTime fechaDocumento = dpFechaDocumento.SelectedDate.Value;

            if (string.IsNullOrWhiteSpace(txtRutaArchivo.Text) || !File.Exists(txtRutaArchivo.Text))
            {
                MessageBox.Show("Debe seleccionar un archivo válido.");
                return;
            }

            var entidades = new List<string>();
            if (chkSUNEDU.IsChecked == true) entidades.Add("SUNEDU");
            if (chkSINEACE.IsChecked == true) entidades.Add("SINEACE");
            if (chkICACIT.IsChecked == true) entidades.Add("ICACIT");
            if (chkISO.IsChecked == true) entidades.Add("ISO 9001");

            // 1. Subir archivo a Drive
            string enlaceDrive = "";
            string driveFileId = "";

            try
            {
                await _driveService.InicializarAsync();
                // Reemplaza esto por el ID real de tu carpeta
                string folderId = "14p_335OMbuBQ4KEhlkXdz4Rt6x6o17zA";

                var (webLink, fileId) = await _driveService.SubirArchivoAsync(txtRutaArchivo.Text, folderId);
                
                // Hacer el archivo público
                await _driveService.HacerArchivoPublicoAsync(fileId);

                enlaceDrive = webLink;
                driveFileId = fileId;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al subir a Drive: " + ex.Message);
                return;
            }

            // 2. Crear y guardar documento
            var doc = new Documento
            {
                Nombre = txtNombre.Text,
                Entidades = entidades,
                Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Año = fechaDocumento.Year,  // Mantienes el campo "Año" como int (opcional)
                FechaDocumento = fechaDocumento,  // Nuevo campo DateTime
                Area = txtArea.Text,
                Usuario = txtUsuario.Text,
                FechaSubida = DateTime.Now,
                EnlaceDrive = enlaceDrive,
                DriveFileId = driveFileId
            };

            _mongoService.Documentos.InsertOne(doc);

            MessageBox.Show("Documento guardado correctamente con archivo en Google Drive.");
            this.Close();
        }

    }
}
