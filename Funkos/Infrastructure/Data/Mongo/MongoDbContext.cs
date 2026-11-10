using Funkos.Models;
using Funkos.Mongo;
using MongoDB.Driver;

namespace Funkos.Mongo;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public IMongoCollection<Proveedor> Proveedores { get; }
    
    public MongoDbContext(IMongoClient client, MongoDbSettings settings)
    {
        // Obtener la base de datos
        Database = client.GetDatabase(settings.DatabaseName);

        // Obtener las colecciones tipadas
        Proveedores = Database.GetCollection<Proveedor>("Proveedores");
        
        var index = Builders<Proveedor>.IndexKeys
            .Ascending(p => p.Name);

        var options = new CreateIndexOptions
        {
            Unique = true
        };

        var model = new CreateIndexModel<Proveedor>(index, options);

        Proveedores.Indexes.CreateOne(model);
    }
}