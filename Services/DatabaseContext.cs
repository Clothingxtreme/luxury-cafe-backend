using MongoDB.Driver;
using MaisonGlace.API.Models;
using MaisonGlace.API.Settings;
using Microsoft.Extensions.Options;

namespace MaisonGlace.API.Services;

public class DatabaseContext
{
    private readonly IMongoDatabase _database;

    public DatabaseContext(IOptions<MongoDbSettings> settings)
    {
        var mongoUrl = MongoUrl.Create(settings.Value.ConnectionString);
        var client = new MongoClient(mongoUrl);
        _database = client.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
