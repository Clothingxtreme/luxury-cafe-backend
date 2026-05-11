using MongoDB.Driver;
using MongoDB.Bson;
using MaisonGlace.API.Models;
using MaisonGlace.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace MaisonGlace.API.Services;

public class DatabaseContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<DatabaseContext> _logger;

    public DatabaseContext(IOptions<MongoDbSettings> options, ILogger<DatabaseContext> logger)
    {
        _logger = logger;
        var cfg = options.Value;
        var mongoUrl = MongoUrl.Create(cfg.ConnectionString);
        var mongoSettings = MongoClientSettings.FromConnectionString(cfg.ConnectionString);

        if (cfg.AllowInsecureTls)
        {
            mongoSettings.AllowInsecureTls = true;
            mongoSettings.SslSettings = new SslSettings
            {
                ServerCertificateValidationCallback = (_, _, _, _) => true
            };
        }

        _logger.LogInformation(
            "Initializing MongoDB client. Host: {Host}; Database: {Database}; Scheme: {Scheme}; InsecureTls: {AllowInsecureTls}",
            mongoUrl.Server?.ToString() ?? "unknown-host",
            string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? "unknown-db" : mongoUrl.DatabaseName,
            mongoUrl.Scheme,
            cfg.AllowInsecureTls);

        var client = new MongoClient(mongoSettings);
        _database = client.GetDatabase(mongoUrl.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    public Task PingAsync(CancellationToken cancellationToken = default) =>
        _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
}
