using AgiloxSortingHall.Data;
using AgiloxSortingHall.Hubs;
using AgiloxSortingHall.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var basePath = AppContext.BaseDirectory;

var logsDirectory = Path.Combine(basePath, "Logs");
var logsPath = Path.Combine(logsDirectory, "log-.txt");

Directory.CreateDirectory(logsDirectory);

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console()
    .WriteTo.File(
        logsPath,
        rollingInterval: RollingInterval.Day,
        retainedFileTimeLimit: TimeSpan.FromDays(14),
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

try
{
    Log.Information("Starting AgiloxSortingHall application...");
    Log.Information("Base path: {BasePath}", basePath);
    Log.Information("Logs path: {LogsPath}", logsPath);

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Host.UseSerilog();

    builder.WebHost.UseUrls("http://0.0.0.0:5000");

    // DbContext s SQLite - databáze vždy relativnì vùèi složce aplikace/publish složce
    var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new Exception("Missing DefaultConnection");

    var sqliteBuilder = new SqliteConnectionStringBuilder(rawConnectionString);

    if (!Path.IsPathRooted(sqliteBuilder.DataSource))
    {
        sqliteBuilder.DataSource = Path.GetFullPath(
            Path.Combine(basePath, sqliteBuilder.DataSource)
        );
    }

    var connectionString = sqliteBuilder.ToString();

    Log.Information("SQLite database path: {DatabasePath}", sqliteBuilder.DataSource);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));

    builder.Services.Configure<HallConfig>(
        builder.Configuration.GetSection("HallConfig"));

    builder.Services.AddTransient<DataSeeder>();

    var agiloxBaseUrl = builder.Configuration["Agilox:BaseUrl"]
        ?? throw new Exception("Missing Agilox BaseUrl in configuration");

    builder.Services.AddHttpClient("Agilox", client =>
    {
        client.BaseAddress = new Uri(agiloxBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(2);
    });

    builder.Services.AddScoped<AgiloxService>();

    // Authentication + Authorization
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "AgiloxSortingHall.Auth";
            options.SlidingExpiration = true;
        });

    builder.Services.AddAuthorization();

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/Administration");
        options.Conventions.AllowAnonymousToPage("/Account/Login");
    });

    builder.Services.AddSignalR();
    builder.Services.AddControllers();

    var app = builder.Build();

    // Migrace / vytvoøení DB pøi startu
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbPath = sqliteBuilder.DataSource;
        var dbDirectory = Path.GetDirectoryName(dbPath);

        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        var dbFileExistsBeforeMigrate = File.Exists(dbPath);

        Log.Information("Database exists before migration: {DatabaseExists}", dbFileExistsBeforeMigrate);

        db.Database.Migrate();

        if (!dbFileExistsBeforeMigrate)
        {
            Log.Information("Database did not exist before migration. Checking whether seed is needed...");

            if (!db.HallRows.Any() && !db.WorkTables.Any())
            {
                Log.Information("Seeding initial data...");

                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAsync();

                Log.Information("Initial data seeded successfully.");
            }
            else
            {
                Log.Information("Seed skipped because database already contains data.");
            }
        }
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapStaticAssets();

    app.MapRazorPages()
       .WithStaticAssets();

    app.MapHub<HallHub>("/hallHub");

    Log.Information("AgiloxSortingHall application started.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AgiloxSortingHall failed to start");
}
finally
{
    Log.CloseAndFlush();
}