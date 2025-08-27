using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Models
{
    public class DocumentoListItem
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Version { get; set; }
        public string FechaDocumento { get; set; }
        public string AreaDependencia { get; set; }
        public string Estado { get; set; }
        public string EnlaceDrive { get; set; }
        public string DriveFileId { get; set; }
    }
}