using GestionCalidad.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionCalidad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAbrirRegistro_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new RegistroDocumento(null, "1", "Rodrigo");
            ventana.ShowDialog();
        }
        private void BtnAbrirListado_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new DashboardPrincipal("1","Rodrigo");
            ventana.ShowDialog();
        }
    }
}