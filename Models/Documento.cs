using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Models
{
    public class Documento
    {
        // Identificador único
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Metadatos del documento
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Codigo { get; set; }
        public string Version { get; set; }
        public string Descripcion { get; set; }

        // Fechas relevantes
        public DateTime FechaDocumento { get; set; }
        public DateTime FechaSubida { get; set; }

        // Clasificación y organización
        [BsonElement("Entidades")]
        public List<string> Entidades { get; set; }
        public string AreaDependencia { get; set; }
        public string Estado { get; set; }

        // Información de usuario
        public string Usuario { get; set; }

        // Integración con Google Drive
        public string EnlaceDrive { get; set; }
        public string DriveFileId { get; set; }
    }
}
