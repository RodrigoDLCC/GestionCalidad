using GestionCalidad.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionCalidad.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        // Constructor
        public MongoService()
        {
            // Cadena de conexión (considera moverla a archivo de configuración en el futuro)
            var connectionString = "mongodb+srv://admin:GestionCalidad2025@gestioncalidadcluster.2fsq5df.mongodb.net/?retryWrites=true&w=majority&appName=GestionCalidadCluster";

            // Nombre de la base de datos que vas a usar (puedes llamarla como quieras)
            var databaseName = "GestionCalidadDB";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Colección de documentos
        public IMongoCollection<Documento> Documentos =>
            _database.GetCollection<Documento>("documentos");

        // Colección de usuarios
        public IMongoCollection<Usuario> Usuarios =>
            _database.GetCollection<Usuario>("usuarios");
    }
}
