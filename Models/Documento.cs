using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Models
{
    [BsonIgnoreExtraElements]
    public class Documento
    {
        // Identificador único
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Metadatos del documento
        public string Nombre { get; set; } = "Sin nombre";
        public string Tipo { get; set; } = "Otros";
        public string Codigo { get; set; } = "Sin código asignado";
        public string Version { get; set; } = "1.0";
        public string Descripcion { get; set; } = "Sin descripción";

        // Fechas relevantes
        public DateTime FechaDocumento { get; set; }
        public DateTime FechaSubida { get; set; }

        // Clasificación y organización
        [BsonElement("Entidades")]
        public List<string> Entidades { get; set; } = new List<string> { "General" };
        public string AreaDependencia { get; set; } = "No especificado";
        public string Estado { get; set; } = "Vigente";

        // Información de usuario
        public string Usuario { get; set; } = "Usuario Desconocido";

        // Integración con Google Drive
        public string EnlaceDrive { get; set; } = string.Empty;
        public string DriveFileId { get; set; } = string.Empty;
    }
}
