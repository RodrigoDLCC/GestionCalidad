using GestionCalidad.Models;
using GestionCalidad.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestionCalidad.Views
{
    public partial class DetalleDocumento : Window
    {
        private readonly Documento _documento;
        private readonly MongoService _mongoService;
        private VersionDocumento _versionSeleccionada;

        public bool DocumentoModificado { get; private set; }

        public DetalleDocumento(Documento documento, MongoService mongoService = null)
        {
            InitializeComponent();

            _documento = documento ?? throw new ArgumentNullException(nameof(documento));
            _mongoService = mongoService ?? new MongoService();

            DataContext = _documento;
            CargarDatosDocumento();
        }

        private void CargarDatosDocumento()
        {
            try
            {
                // Cargar selector de versiones
                CargarVersiones();

                // Cargar información de entidades
                CargarEntidades();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los detalles: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CargarEntidades()
        {
            try
            {
                if (_documento.EntidadesIds != null && _documento.EntidadesIds.Any())
                {
                    var nombresEntidades = new List<string>();

                    foreach (var entidadId in _documento.EntidadesIds)
                    {
                        var entidad = await _mongoService.ObtenerEntidadPorIdAsync(entidadId);
                        if (entidad != null)
                        {
                            nombresEntidades.Add(entidad.Nombre);
                        }
                    }

                    TxtEntidades.Text = string.Join(", ", nombresEntidades);
                }
                else
                {
                    TxtEntidades.Text = "No especificado";
                }
            }
            catch (Exception ex)
            {
                TxtEntidades.Text = "Error al cargar entidades";
                Console.WriteLine($"Error al cargar entidades: {ex.Message}");
            }
        }

        private void CargarVersiones()
        {
            try
            {
                CmbVersiones.Items.Clear();

                if (_documento.HistorialVersiones != null && _documento.HistorialVersiones.Any())
                {
                    // Ordenar versiones de más reciente a más antigua
                    var versionesOrdenadas = _documento.HistorialVersiones
                        .OrderByDescending(v => v.FechaCreacion)
                        .ToList();

                    foreach (var version in versionesOrdenadas)
                    {
                        CmbVersiones.Items.Add(version.NumeroVersion);
                    }

                    // Seleccionar la versión actual por defecto
                    var versionActual = versionesOrdenadas.FirstOrDefault(v =>
                        v.NumeroVersion == _documento.VersionActual) ?? versionesOrdenadas.First();

                    CmbVersiones.SelectedItem = versionActual.NumeroVersion;
                    MostrarInformacionVersion(versionActual);
                }
                else
                {
                    // Si no hay historial, mostrar versión actual
                    var versionActual = new VersionDocumento
                    {
                        NumeroVersion = _documento.VersionActual,
                        Cambios = "Versión inicial",
                        FechaCreacion = _documento.FechaSubida,
                        UsuarioNombre = _documento.UsuarioNombre
                    };

                    CmbVersiones.Items.Add(_documento.VersionActual);
                    CmbVersiones.SelectedIndex = 0;
                    MostrarInformacionVersion(versionActual);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las versiones: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarInformacionVersion(VersionDocumento version)
        {
            _versionSeleccionada = version;

            TxtCambiosVersion.Text = version.Cambios ?? "Sin cambios registrados";
            TxtFechaVersion.Text = version.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
            TxtCreadorVersion.Text = version.UsuarioNombre ?? _documento.UsuarioNombre;
        }

        private void CmbVersiones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbVersiones.SelectedItem == null) return;

            var numeroVersion = CmbVersiones.SelectedItem.ToString();
            var version = _documento.HistorialVersiones?
                .FirstOrDefault(v => v.NumeroVersion == numeroVersion);

            if (version != null)
            {
                MostrarInformacionVersion(version);
            }
        }

        private async void BtnDescargarVersion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_versionSeleccionada == null)
                {
                    MessageBox.Show("No hay versión seleccionada para descargar.",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var googleDriveService = new GoogleDriveService();
                await googleDriveService.EnsureInitializedAsync();

                var archivoInfo = await googleDriveService.ObtenerArchivoAsync(_versionSeleccionada.DriveFileId);
                var extension = ObtenerExtensionDesdeMimeType(archivoInfo.MimeType);

                var nombreArchivo = $"{_documento.Nombre}_v{_versionSeleccionada.NumeroVersion}{extension}";

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = nombreArchivo,
                    Filter = $"Archivos {extension.ToUpper().TrimStart('.')}|*{extension}|Todos los archivos (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var fileStream = await googleDriveService.DescargarArchivoAsync(_versionSeleccionada.DriveFileId))
                    using (var outputStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                    {
                        await fileStream.CopyToAsync(outputStream);
                    }

                    MessageBox.Show("Versión descargada correctamente.",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al descargar la versión: {ex.Message}",
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

        private async void BtnVerEnDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_versionSeleccionada == null)
                {
                    MessageBox.Show("No hay versión seleccionada para visualizar.",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Obtener el enlace de visualización para la versión seleccionada
                string enlaceVisualizacion = await ObtenerEnlaceVisualizacionAsync(_versionSeleccionada.DriveFileId);

                if (!string.IsNullOrEmpty(enlaceVisualizacion))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = enlaceVisualizacion,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("No hay enlace disponible para esta versión.",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir en Google Drive: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> ObtenerEnlaceVisualizacionAsync(string driveFileId)
        {
            try
            {
                var googleDriveService = new GoogleDriveService();
                await googleDriveService.EnsureInitializedAsync();

                // Obtener información del archivo para determinar el mejor visor
                var archivoInfo = await googleDriveService.ObtenerArchivoAsync(driveFileId);

                return ObtenerEnlacePorTipoArchivo(driveFileId, archivoInfo.MimeType);
            }
            catch
            {
                // Fallback: enlace básico de visualización
                return $"https://drive.google.com/file/d/{driveFileId}/view";
            }
        }

        private string ObtenerEnlacePorTipoArchivo(string driveFileId, string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return $"https://drive.google.com/file/d/{driveFileId}/view";

            // Documentos de Google
            if (mimeType.Contains("application/vnd.google-apps"))
            {
                if (mimeType.Contains("document"))
                    return $"https://docs.google.com/document/d/{driveFileId}/edit";
                else if (mimeType.Contains("spreadsheet"))
                    return $"https://docs.google.com/spreadsheets/d/{driveFileId}/edit";
                else if (mimeType.Contains("presentation"))
                    return $"https://docs.google.com/presentation/d/{driveFileId}/edit";
            }

            // Archivos visibles directamente (PDF, imágenes, texto)
            else if (mimeType == "application/pdf" ||
                     mimeType.StartsWith("image/") ||
                     mimeType.StartsWith("text/"))
            {
                return $"https://drive.google.com/file/d/{driveFileId}/view";
            }

            // Archivos de Office (Word, Excel, PowerPoint)
            else if (mimeType.Contains("wordprocessingml") ||
                     mimeType.Contains("spreadsheetml") ||
                     mimeType.Contains("presentationml"))
            {
                return $"https://docs.google.com/viewer?url=https://drive.google.com/uc?id={driveFileId}";
            }

            // Para cualquier otro tipo de archivo
            return $"https://drive.google.com/file/d/{driveFileId}/view";
        }

        private async void BtnNuevaVersion_Click(object sender, RoutedEventArgs e)
        {
            var nuevaVersionWindow = new NuevaVersionDocumento(_documento, _mongoService);
            nuevaVersionWindow.Owner = this;

            if (nuevaVersionWindow.ShowDialog() == true)
            {
                try
                {
                    // Mostrar indicador de carga
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Recargar solo el historial de versiones desde la BD
                    var documentoActualizado = await _mongoService.ObtenerDocumentoPorIdAsync(_documento.Id);
                    if (documentoActualizado != null && documentoActualizado.HistorialVersiones != null)
                    {
                        _documento.HistorialVersiones = documentoActualizado.HistorialVersiones;
                        _documento.VersionActual = documentoActualizado.VersionActual;
                        _documento.FechaUltimaModificacion = documentoActualizado.FechaUltimaModificacion;

                        // Actualizar el combobox de versiones
                        CargarVersiones();

                        DocumentoModificado = true;

                        MessageBox.Show("Nueva versión registrada exitosamente.",
                                      "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al actualizar la lista de versiones: {ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Si se cierra con X, establecer DialogResult
            if (this.DialogResult == null)
            {
                this.DialogResult = true;
            }
        }
    }
}