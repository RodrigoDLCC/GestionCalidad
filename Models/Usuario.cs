using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }  // Guarda hash de la contraseña (nunca texto plano)

        public string Rol { get; set; }           // Ej: "admin", "editor", "lector"
    }
}
