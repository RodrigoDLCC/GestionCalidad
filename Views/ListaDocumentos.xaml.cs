using GestionCalidad.Models;
using GestionCalidad.Services;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionCalidad.Views
{
    public partial class ListaDocumentos : Window
    {
        private readonly MongoService _mongoService;
        private List<Documento> _documentosCompletos;
        private readonly Entidad _entidadFiltro;
        private readonly string _usuarioId;
        private readonly string _usuarioActual;

        public string TituloEntidad => _entidadFiltro == null
            ? "TODOS LOS DOCUMENTOS"
            : $"DOCUMENTOS DE {_entidadFiltro.Nombre.ToUpper()}";

        public string TituloVentana => _entidadFiltro == null
            ? "Todos los Documentos - Sistema de Gestión de Calidad"
            : $"{_entidadFiltro.Nombre} - Documentos";

        public ListaDocumentos(Entidad entidadFiltro, string usuarioId, string usuarioActual)
        {
            InitializeComponent();

            _entidadFiltro = entidadFiltro;
            _usuarioId = usuarioId;
            _usuarioActual = usuarioActual;

            DataContext = this;
            _mongoService = new MongoService();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDocumentosAsync();
            await CargarFiltrosAsync();
        }

        private async System.Threading.Tasks.Task CargarDocumentosAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                if (_entidadFiltro == null) // TODOS los documentos
                {
                    _documentosCompletos = await _mongoService.Documentos
                        .Find(d => d.Estado != "Eliminado")
                        .SortByDescending(d => d.FechaUltimaModificacion)
                        .ToListAsync();
                }
                else // Documentos de una entidad específica
                {
                    _documentosCompletos = await _mongoService.ObtenerDocumentosPorEntidadAsync(_entidadFiltro.Id);
                }

                ActualizarListado(_documentosCompletos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar documentos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task CargarFiltrosAsync()
        {
            try
            {
                // Cargar tipos de documento únicos desde la base de datos
                var tipos = await _mongoService.ObtenerTiposDeDocumentoAsync();

                // Puedes agregar lógica adicional para poblar combobox si es necesario
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar filtros: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActualizarListado(List<Documento> documentos)
        {
            var items = documentos.Select(d => new DocumentoListItem
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Tipo = d.Tipo,
                Version = d.VersionActual,
                FechaDocumento = d.FechaDocumento.ToString("dd/MM/yyyy"),
                AreaDependencia = d.AreaDependencia,
                Estado = d.Estado,
                EnlaceDrive = d.EnlaceDrive,
                DriveFileId = d.DriveFileId
            }).ToList();

            dgDocumentos.ItemsSource = items;
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void TxtBusqueda_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AplicarFiltros();
            }
        }

        private void AplicarFiltros()
        {
            if (_documentosCompletos == null) return;

            var documentosFiltrados = _documentosCompletos.AsEnumerable();

            // Filtro por tipo
            if (cmbFiltroTipo.SelectedIndex > 0)
            {
                var tipoSeleccionado = (cmbFiltroTipo.SelectedItem as ComboBoxItem)?.Content.ToString();
                documentosFiltrados = documentosFiltrados.Where(d => d.Tipo == tipoSeleccionado);
            }

            // Filtro por estado
            if (cmbFiltroEstado.SelectedIndex > 0)
            {
                var estadoSeleccionado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content.ToString();
                documentosFiltrados = documentosFiltrados.Where(d => d.Estado == estadoSeleccionado);
            }

            // Filtro por búsqueda de texto
            if (!string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                var busqueda = txtBusqueda.Text.ToLower();
                documentosFiltrados = documentosFiltrados.Where(d =>
                    d.Nombre.ToLower().Contains(busqueda) ||
                    (d.Descripcion != null && d.Descripcion.ToLower().Contains(busqueda)) ||
                    (d.Codigo != null && d.Codigo.ToLower().Contains(busqueda)));
            }

            ActualizarListado(documentosFiltrados.ToList());
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbFiltroTipo.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndex = 0;
            txtBusqueda.Clear();

            ActualizarListado(_documentosCompletos);
        }

        private void BtnVerDocumento_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var documentoId = button?.Tag?.ToString();

            if (string.IsNullOrEmpty(documentoId))
            {
                MessageBox.Show("No se pudo identificar el documento seleccionado.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var documento = _documentosCompletos.FirstOrDefault(d => d.Id == documentoId);
                if (documento == null)
                {
                    MessageBox.Show("Documento no encontrado.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var detalleWindow = new DetalleDocumento(documento, _mongoService);
                detalleWindow.Owner = this;
                detalleWindow.ShowDialog();

                // Recargar si hubo cambios
                if (detalleWindow.DocumentoModificado)
                {
                    Window_Loaded(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el documento: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDescargarDocumento_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var driveFileId = button?.Tag?.ToString();

            if (string.IsNullOrEmpty(driveFileId))
            {
                MessageBox.Show("No se puede descargar el documento. ID no válido.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var documento = _documentosCompletos.FirstOrDefault(d => d.DriveFileId == driveFileId);
                if (documento == null)
                {
                    MessageBox.Show("Documento no encontrado.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var googleDriveService = new GoogleDriveService();
                await googleDriveService.EnsureInitializedAsync();

                var archivoInfo = await googleDriveService.ObtenerArchivoAsync(driveFileId);
                var extension = ObtenerExtensionDesdeMimeType(archivoInfo.MimeType);

                var nombreArchivo = $"{documento.Nombre}{extension}";

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = nombreArchivo,
                    Filter = $"Archivos {extension.ToUpper().TrimStart('.')}|*{extension}|Todos los archivos (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var fileStream = await googleDriveService.DescargarArchivoAsync(driveFileId))
                    using (var outputStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                    {
                        await fileStream.CopyToAsync(outputStream);
                    }

                    MessageBox.Show("Documento descargado correctamente.",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al descargar el documento: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ObtenerExtensionDesdeMimeType(string mimeType)
        {
            var mimeToExtension = new Dictionary<string, string>
            {
                {"application/pdf", ".pdf"},
                {"application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx"},
                {"application/msword", ".doc"},
                {"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"},
                {"application/vnd.ms-excel", ".xls"},
                {"application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx"},
                {"application/vnd.ms-powerpoint", ".ppt"},
                {"text/plain", ".txt"},
                {"image/jpeg", ".jpg"},
                {"image/png", ".png"},
                {"image/gif", ".gif"},
                {"application/zip", ".zip"},
                {"application/x-rar-compressed", ".rar"}
            };

            return mimeToExtension.ContainsKey(mimeType) ? mimeToExtension[mimeType] : "";
        }

        private void BtnEditarDocumento_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var documentoId = button?.Tag?.ToString();

            if (string.IsNullOrEmpty(documentoId))
            {
                MessageBox.Show("No se pudo identificar el documento seleccionado.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var documento = _documentosCompletos.FirstOrDefault(d => d.Id == documentoId);
                if (documento == null)
                {
                    MessageBox.Show("Documento no encontrado.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var editarWindow = new EditarDocumento(documento, _mongoService);
                editarWindow.Owner = this;
                editarWindow.ShowDialog();

                // Recargar si hubo cambios
                if (editarWindow.DocumentoModificado)
                {
                    Window_Loaded(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar el documento: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNuevoDocumento_Click(object sender, RoutedEventArgs e)
        {
            var registroWindow = new RegistroDocumento(_entidadFiltro, _usuarioId, _usuarioActual);
            registroWindow.Owner = this;
            registroWindow.ShowDialog();

            // Recargar documentos después de cerrar
            if (registroWindow.DocumentoRegistrado)
            {
                Window_Loaded(null, null);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardPrincipal(_usuarioId, _usuarioActual);
            dashboard.Show();
            this.Close();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}