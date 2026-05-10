namespace MaisonGlace.API.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool AllowInsecureTls { get; set; } = false;
}
