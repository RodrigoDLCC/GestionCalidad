using GestionCalidad.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCalidad.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        // Constructor mejorado con configuración
        public MongoService()
        {
            try
            {
                // Considera mover la cadena de conexión a App.config en el futuro
                var connectionString = ConfigurationManager.AppSettings["MongoDBConnectionString"]
                    ?? "mongodb+srv://admin:GestionCalidad2025@gestioncalidadcluster.2fsq5df.mongodb.net/?retryWrites=true&w=majority&appName=GestionCalidadCluster";

                var databaseName = ConfigurationManager.AppSettings["MongoDBDatabaseName"]
                    ?? "GestionCalidadDB";

                var settings = MongoClientSettings.FromConnectionString(connectionString);

                var client = new MongoClient(settings);
                _database = client.GetDatabase(databaseName);

                // Crear índices para mejorar el rendimiento
                CrearIndices();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al conectar con MongoDB: " + ex.Message, ex);
            }
        }

        private async void CrearIndices()
        {
            // Índices para la colección de documentos
            var documentosIndexKeys = Builders<Documento>.IndexKeys
                .Ascending(d => d.EntidadesIds)
                .Ascending(d => d.Estado)
                .Ascending(d => d.Tipo);

            var documentosIndexOptions = new CreateIndexOptions { Name = "Entidades_Estado_Tipo" };
            await Documentos.Indexes.CreateOneAsync(new CreateIndexModel<Documento>(documentosIndexKeys, documentosIndexOptions));

            // Índices para la colección de entidades
            var entidadesIndexKeys = Builders<Entidad>.IndexKeys.Ascending(e => e.Activo);
            var entidadesIndexOptions = new CreateIndexOptions { Name = "Activo" };
            await Entidades.Indexes.CreateOneAsync(new CreateIndexModel<Entidad>(entidadesIndexKeys, entidadesIndexOptions));
        }

        // Colecciones
        public IMongoCollection<Documento> Documentos =>
            _database.GetCollection<Documento>("documentos");

        public IMongoCollection<Entidad> Entidades =>
            _database.GetCollection<Entidad>("entidades");

        public IMongoCollection<Usuario> Usuarios =>
            _database.GetCollection<Usuario>("usuarios");

        // Métodos para Entidades
        public async Task<List<Entidad>> ObtenerTodasEntidadesAsync(bool soloActivas = true)
        {
            var filter = soloActivas
                ? Builders<Entidad>.Filter.Eq(e => e.Activo, true)
                : Builders<Entidad>.Filter.Empty;

            return await Entidades.Find(filter)
                .SortBy(e => e.Orden)
                .ThenBy(e => e.Nombre)
                .ToListAsync();
        }

        public async Task<Entidad> ObtenerEntidadPorIdAsync(string id)
        {
            return await Entidades.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Entidad> ObtenerEntidadPorNombreAsync(string nombre)
        {
            return await Entidades.Find(e => e.Nombre == nombre).FirstOrDefaultAsync();
        }

        public async Task CrearEntidadAsync(Entidad entidad)
        {
            await Entidades.InsertOneAsync(entidad);
        }

        public async Task<bool> ActualizarEntidadAsync(string id, Entidad entidad)
        {
            var result = await Entidades.ReplaceOneAsync(e => e.Id == id, entidad);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        // Métodos para Documentos
        public async Task<List<Documento>> ObtenerDocumentosPorEntidadAsync(string entidadId)
        {
            return await Documentos.Find(d => d.EntidadesIds.Contains(entidadId) && d.Estado != "Eliminado")
                .SortByDescending(d => d.FechaUltimaModificacion)
                .ToListAsync();
        }

        public async Task<List<Documento>> ObtenerDocumentosPorEntidadesAsync(List<string> entidadesIds)
        {
            return await Documentos.Find(d => d.EntidadesIds.Any(id => entidadesIds.Contains(id)) && d.Estado != "Eliminado")
                .SortByDescending(d => d.FechaUltimaModificacion)
                .ToListAsync();
        }

        public async Task<Documento> ObtenerDocumentoPorIdAsync(string id)
        {
            return await Documentos.Find(d => d.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Documento> ObtenerDocumentoPorDriveIdAsync(string driveFileId)
        {
            return await Documentos.Find(d => d.DriveFileId == driveFileId).FirstOrDefaultAsync();
        }

        public async Task<string> CrearDocumentoAsync(Documento documento)
        {
            await Documentos.InsertOneAsync(documento);
            return documento.Id;
        }

        public async Task<bool> ActualizarDocumentoAsync(string id, Documento documento)
        {
            documento.FechaUltimaModificacion = DateTime.Now;
            var result = await Documentos.ReplaceOneAsync(d => d.Id == id, documento);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> AgregarVersionADocumentoAsync(string documentoId, VersionDocumento version)
        {
            var filter = Builders<Documento>.Filter.Eq(d => d.Id, documentoId);
            var update = Builders<Documento>.Update
                .Push(d => d.HistorialVersiones, version)
                .Set(d => d.VersionActual, version.NumeroVersion)
                .Set(d => d.FechaUltimaModificacion, DateTime.Now);

            var result = await Documentos.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> CambiarEstadoDocumentoAsync(string documentoId, string nuevoEstado)
        {
            var filter = Builders<Documento>.Filter.Eq(d => d.Id, documentoId);
            var update = Builders<Documento>.Update
                .Set(d => d.Estado, nuevoEstado)
                .Set(d => d.FechaUltimaModificacion, DateTime.Now);

            var result = await Documentos.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        // Métodos de búsqueda avanzada
        public async Task<List<Documento>> BuscarDocumentosAsync(string searchTerm, string entidadId = null, string tipo = null, string estado = null)
        {
            var filterBuilder = Builders<Documento>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(d => d.Estado, estado ?? "Vigente"),
                filterBuilder.Or(
                    filterBuilder.Regex(d => d.Nombre, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(d => d.Descripcion, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(d => d.Codigo, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                )
            );

            if (!string.IsNullOrEmpty(entidadId))
            {
                filter = filter & filterBuilder.AnyEq(d => d.EntidadesIds, entidadId);
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                filter = filter & filterBuilder.Eq(d => d.Tipo, tipo);
            }

            return await Documentos.Find(filter)
                .SortByDescending(d => d.FechaUltimaModificacion)
                .ToListAsync();
        }

        // Métodos de reporte y estadísticas
        public async Task<long> ContarDocumentosPorEntidadAsync(string entidadId)
        {
            return await Documentos.CountDocumentsAsync(d =>
                d.EntidadesIds.Contains(entidadId) && d.Estado != "Eliminado");
        }

        public async Task<List<string>> ObtenerTiposDeDocumentoAsync()
        {
            return await Documentos.Distinct<string>("Tipo", Builders<Documento>.Filter.Empty).ToListAsync();
        }
    }
}