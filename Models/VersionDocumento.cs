using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Models
{
    public class VersionDocumento
    {
        public string NumeroVersion { get; set; }
        public string DriveFileId { get; set; }
        public string Cambios { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public string UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }

        // Campos adicionales para control
        public string Comentarios { get; set; }
        public bool EsVersionFinal { get; set; } = false;
    }
}