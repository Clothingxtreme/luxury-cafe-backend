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
        var cfg = options.Value;
        var mongoSettings = MongoClientSettings.FromConnectionString(cfg.ConnectionString);
        var sslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12,
            CheckCertificateRevocation = false
        };

        if (cfg.AllowInsecureTls)
        {
            sslSettings.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        mongoSettings.SslSettings = sslSettings;
        mongoSettings.AllowInsecureTls = cfg.AllowInsecureTls;

        var mongoUrl = MongoUrl.Create(cfg.ConnectionString);
        var client = new MongoClient(mongoSettings);
        _database = client.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
