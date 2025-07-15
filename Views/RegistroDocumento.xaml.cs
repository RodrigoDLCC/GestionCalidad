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
    /// Lógica de interacción para RegistroDocumento.xaml
    /// </summary>
    public partial class RegistroDocumento : Window
    {
        private readonly MongoService _mongoService;

        public RegistroDocumento()
        {
            InitializeComponent();
            _mongoService = new MongoService();  // usar tu servicio de Mongo
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar año
            if (!int.TryParse(txtAnio.Text, out int anio))
            {
                MessageBox.Show("El año debe ser un número válido.");
                return;
            }

            var entidades = new List<string>();
            if (chkSUNEDU.IsChecked == true) entidades.Add("SUNEDU");
            if (chkSINEACE.IsChecked == true) entidades.Add("SINEACE");
            if (chkICACIT.IsChecked == true) entidades.Add("ICACIT");
            if (chkISO.IsChecked == true) entidades.Add("ISO 9001");

            var doc = new Documento
            {
                Nombre = txtNombre.Text,
                Entidades = entidades,
                Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Año = anio,
                Area = txtArea.Text,
                Usuario = txtUsuario.Text,
                FechaSubida = DateTime.Now,
                EnlaceDrive = null,        // se llenará después al subir a Drive
                DriveFileId = null
            };

            _mongoService.Documentos.InsertOne(doc);

            MessageBox.Show("Documento guardado correctamente en MongoDB");
            this.Close();
        }
    }
}
