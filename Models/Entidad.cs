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
    public class Entidad
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string ImagenUrl { get; set; }
        public string GoogleDriveFolderId { get; set; }

        // Campos adicionales para mejor organización
        public string Codigo { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; } = 0;
    }
}