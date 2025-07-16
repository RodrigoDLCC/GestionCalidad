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
        public List<string> Entidades { get; set; }

        public string Tipo { get; set; }
        public int Año { get; set; }
        public DateTime FechaDocumento { get; set; }
        public string Area { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaSubida { get; set; }

        public string EnlaceDrive { get; set; }
        public string DriveFileId { get; set; }
    }
}
