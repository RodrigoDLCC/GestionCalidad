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
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Nombre { get; set; }

        [BsonElement("Entidades")]
        public List<string> Entidades { get; set; }  // Ej: ["SUNEDU", "ISO 9001"]

        public string Tipo { get; set; }             // Manual, Informe, etc.
        public int Año { get; set; }
        public string Area { get; set; }
        public string Usuario { get; set; }          // Usuario que subió el archivo
        public DateTime FechaSubida { get; set; }

        public string EnlaceDrive { get; set; }
        public string DriveFileId { get; set; }
    }
}
