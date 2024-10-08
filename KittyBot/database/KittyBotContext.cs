using Microsoft.EntityFrameworkCore;

namespace KittyBot.database;

public class KittyBotContext: DbContext
{
    private const string UsernameEnv = "KITTY_PS_USERNAME";
    private const string PasswordEnv = "KITTY_PS_PASSWORD";
    private const string HostnameEnv = "KITTY_PS_HOSTNAME";
    private const string DatabaseEnv = "KITTY_PS_DATABASE";
    
    public KittyBotContext(DbContextOptions<KittyBotContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(GetConnectionString());
    }

    public static string GetConnectionString()
    {
        string? username = Environment.GetEnvironmentVariable(UsernameEnv);
        if (username == null)
        {
            username = "postgres";
        }
        string? password = Environment.GetEnvironmentVariable(PasswordEnv);
        if (password == null)
        {
            password = "";
        }
        string? hostname = Environment.GetEnvironmentVariable(HostnameEnv);
        if (hostname == null)
        {
            hostname = "localhost";
        }
        string? database = Environment.GetEnvironmentVariable(DatabaseEnv);
        if (database == null)
        {
            database = username;
        }
        return $"User Id={username};Password={password};Host={hostname};Database={database};";
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Event> Events { get; set; }
    
    public DbSet<Birthday> Birthdays { get; set; }
    
    public DbSet<HistoricalMessage> Messages { get; set; }
    
    public DbSet<Stats> Stats { get; set; }
    
    public DbSet<ChatLanguage> ChatsLanguages { get; set; }
    
    public DbSet<ModelAnalytic> ModelsAnalytics { get; set; }

    public DbSet<ResponseConfig> ResponseConfigs { get; set; }
}