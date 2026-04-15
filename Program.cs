using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<EventSettings>(builder.Configuration.GetSection("EventSettings"));
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AwsSettings"));

builder.Services.AddSingleton<IEmailService>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    if (env.IsDevelopment())
    {
        var logger = sp.GetRequiredService<ILogger<ConsoleEmailService>>();
        return new ConsoleEmailService(logger);
    }
    var awsSettings = sp.GetRequiredService<IOptions<AwsSettings>>();
    var sesLogger = sp.GetRequiredService<ILogger<SesEmailService>>();
    return new SesEmailService(awsSettings, sesLogger);
});

builder.Services.AddScoped<IAttendanceService, AttendanceService>();

var app = builder.Build();

await SeedDatabaseAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var eventSettings = scope.ServiceProvider.GetRequiredService<IOptions<EventSettings>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await db.Database.EnsureCreatedAsync();

    if (!await db.Sessions.AnyAsync())
    {
        foreach (var s in eventSettings.Sessions)
        {
            db.Sessions.Add(new Session
            {
                Name = s.Name,
                StartTime = DateTime.Parse(s.StartTime),
                EndTime = DateTime.Parse(s.EndTime)
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} sessions", eventSettings.Sessions.Count);
    }
}
