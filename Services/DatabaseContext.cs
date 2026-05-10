using MongoDB.Driver;
using MaisonGlace.API.Models;
using MaisonGlace.API.Settings;
using Microsoft.Extensions.Options;
using System.Security.Authentication;

namespace MaisonGlace.API.Services;

public class DatabaseContext
{
    private readonly IMongoDatabase _database;

    public DatabaseContext(IOptions<MongoDbSettings> options)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        mongoSettings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };

        var mongoUrl = MongoUrl.Create(options.Value.ConnectionString);
        var client = new MongoClient(mongoSettings);
        _database = client.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
