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
    /// Lógica de interacción para DashboardPrincipal.xaml
    /// </summary>
    public partial class DashboardPrincipal : Window
    {
        private string _usuarioActual;

        public DashboardPrincipal(string nombreUsuario)
        {
            InitializeComponent();
            _usuarioActual = nombreUsuario;
            txtUsuario.Text = $"Usuario: {_usuarioActual}";
        }

        private void EntityButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string entidadSeleccionada = button.Tag.ToString();

            var listaDocumentos = new ListaDocumentos(entidadSeleccionada, _usuarioActual);
            listaDocumentos.Show();
            this.Hide();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}