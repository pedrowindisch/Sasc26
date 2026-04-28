using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;
using Sasc26.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "Sasc26.Admin";
});

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

// Register IEventContext as scoped - resolves the current event from route/session
builder.Services.AddScoped<IEventContext, EventContext>();

builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IVolunteerService, VolunteerService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IThankYouService, ThankYouService>();

var app = builder.Build();

await SeedDatabaseAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();

// SuperAdmin route - must come before the event slug route to avoid
// matching "SuperAdmin" as an event slug
app.MapControllerRoute("superadmin", "SuperAdmin/{action=Index}/{id?}",
    defaults: new { controller = "SuperAdmin" })
    .WithStaticAssets();

// Route with event slug: /{slug}/controller/action
app.MapControllerRoute("event", "{eventSlug}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Default route - redirects to the first active event's homepage
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var eventSettings = scope.ServiceProvider.GetRequiredService<IOptions<EventSettings>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await db.Database.MigrateAsync();

    // Seed default event if none exists
    if (!await db.Events.AnyAsync())
    {
        var defaultEvent = new Event
        {
            Slug = "sasc26",
            Name = "SASC 26",
            Subtitle = "Semana Acadêmica de Sistemas de Computação",
            AllowedEmailDomain = eventSettings.AllowedEmailDomain,
            InstagramUrl = eventSettings.InstagramUrl,
            TshirtPresaleUrl = eventSettings.TshirtPresaleUrl,
            AdminEmailsJson = System.Text.Json.JsonSerializer.Serialize(eventSettings.AdminEmails),
            PostCheckinButtonsJson = "[]",
            PrimaryColor = "#113D76",
            AccentColor = "#1a1a1a",
            BackgroundColor = "#ffffff",
            TextColor = "#1a1a1a"
        };
        db.Events.Add(defaultEvent);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default event: {Slug}", defaultEvent.Slug);
    }

    // Get the default event
    var ev = await db.Events.FirstOrDefaultAsync(e => e.Slug == "sasc26")
             ?? await db.Events.FirstAsync();

    // Seed time slots for the default event
    if (!await db.TimeSlots.AnyAsync())
    {
        foreach (var ts in eventSettings.TimeSlots)
        {
            db.TimeSlots.Add(new TimeSlot
            {
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                Shift = ts.Shift,
                CreditHours = ts.CreditHours > 0 ? ts.CreditHours : 2,
                EventId = ev.Id
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} time slots for event {Slug}", eventSettings.TimeSlots.Count, ev.Slug);
    }

    // Seed certificate config for the default event
    if (!await db.CertificateConfigs.AnyAsync())
    {
        db.CertificateConfigs.Add(new CertificateConfig
        {
            EventId = ev.Id,
            TitleColor = "#113D76",
            BodyColor = "#1a1a1a",
            BorderColor = "#113D76"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default certificate config for event {Slug}", ev.Slug);
    }

    // Seed thank you config for the default event
    if (!await db.ThankYouConfigs.AnyAsync())
    {
        db.ThankYouConfigs.Add(new ThankYouConfig
        {
            EventId = ev.Id,
            Message = $"Obrigado por participar da {ev.Name}!",
            IsFormEnabled = false,
            FormFields = "[]"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default thank you config for event {Slug}", ev.Slug);
    }
}
