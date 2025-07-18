using GestionCalidad.Models;
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
    /// Lógica de interacción para DetalleDocumento.xaml
    /// </summary>
    public partial class DetalleDocumento : Window
    {
        private readonly Documento _documento;

        public DetalleDocumento(Documento documento)
        {
            InitializeComponent();
            _documento = documento;
            CargarDatosDocumento();
        }

        private void CargarDatosDocumento()
        {
            // Información básica
            txtNombre.Text = _documento.Nombre;
            txtDescripcion.Text = _documento.Descripcion ?? "Sin descripción";
            txtCodigo.Text = _documento.Codigo ?? "No especificado";
            txtTipo.Text = _documento.Tipo;
            txtVersion.Text = _documento.Version;
            txtEstado.Text = _documento.Estado;

            // Fechas y ubicación
            txtFechaDocumento.Text = _documento.FechaDocumento.ToString("dd/MM/yyyy");
            txtFechaSubida.Text = _documento.FechaSubida.ToString("dd/MM/yyyy HH:mm");
            txtAreaDependencia.Text = _documento.AreaDependencia;
            txtUsuario.Text = _documento.Usuario;

            // Entidades relacionadas
            itemsEntidades.ItemsSource = _documento.Entidades;

            // Enlace a Drive
            txtEnlaceDrive.Text = _documento.EnlaceDrive;
        }

        private void BtnAbrirDrive_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_documento.EnlaceDrive))
            {
                try
                {
                    System.Diagnostics.Process.Start(_documento.EnlaceDrive);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el enlace: {ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
