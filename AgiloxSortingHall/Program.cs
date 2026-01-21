using AgiloxSortingHall.Data;
using AgiloxSortingHall.Hubs;
using AgiloxSortingHall.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build()
    )
    .CreateLogger();

try
{
    Log.Information("Starting AgiloxSortingHall application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.WebHost.UseUrls("http://0.0.0.0:5000");

    // DbContext s SQLite
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.Configure<HallConfig>(
        builder.Configuration.GetSection("HallConfig"));
    builder.Services.AddTransient<DataSeeder>();

    var agiloxBaseUrl = builder.Configuration["Agilox:BaseUrl"]
                         ?? throw new Exception("Missing Agilox BaseUrl in configuration");

    builder.Services.AddHttpClient("Agilox", client =>
    {
        client.BaseAddress = new Uri(agiloxBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });


    builder.Services.AddScoped<AgiloxService>();


    // Add services to the container.
    builder.Services.AddRazorPages();

    builder.Services.AddSignalR();

    builder.Services.AddControllers();

    var app = builder.Build();

    // Migrace / vytvo¯enÌ DB p¯i startu
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // zjistit cestu k sqlite souboru z connection stringu
        var cs = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new Exception("Missing DefaultConnection");

        var csb = new SqliteConnectionStringBuilder(cs);
        var dataSource = csb.DataSource;

        // u relativnÌ cesty ji ukotvi do base directory aplikace (aù kontrola File.Exists sedÌ)
        var dbPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource));

        var dbFileExistsBeforeMigrate = File.Exists(dbPath);

        db.Database.Migrate();

        // Seed jen pokud DB soubor p¯ed migracÌ neexistoval
        if (!dbFileExistsBeforeMigrate)
        {
            if (!db.HallRows.Any() && !db.WorkTables.Any())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAsync();
            }
        }
    }


    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    //app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.MapControllers();

    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets();

    app.MapHub<HallHub>("/hallHub");

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
