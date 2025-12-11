using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<VivuqeQRSystem.Services.IAuditService, VivuqeQRSystem.Services.AuditService>();
    
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Add Data Protection for persistent keys
var keysPath = builder.Environment.IsDevelopment() 
    ? Path.Combine(Directory.GetCurrentDirectory(), "keys") 
    : "/data/keys";
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

// Configure Kestrel - use environment variable in production, fallback to 5002 for development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5002");
}

// Configure DB Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsProduction())
{
    // Use the persistent volume path in production
    connectionString = "Data Source=/data/vivuqe.db";
}
else if (string.IsNullOrEmpty(connectionString))
{
    // Fallback for dev
    connectionString = "Data Source=vivuqe.db";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));


var app = builder.Build();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Try to apply migrations, fallback to EnsureCreated for existing databases
    try
    {
        db.Database.Migrate();
    }
    catch
    {
        // If migration fails (e.g., database was created with EnsureCreated), just ensure tables exist
        db.Database.EnsureCreated();
    }

    var seedEnabled = app.Configuration.GetValue<bool?>("SeedOnStartup") ?? false;
    var isDev = app.Environment.IsDevelopment();
    if (seedEnabled && isDev && !db.Seniors.Any())
    {
        var senior = new Senior
        {
            Name = "Sample Senior",
            NumberOfGuests = 3,
            Guests = new List<Guest>
            {
                new Guest { Name = "Guest 1" },
                new Guest { Name = "Guest 2" },
                new Guest { Name = "Guest 3" }
            }
        };
        db.Seniors.Add(senior);
        db.SaveChanges();
    }

    // Seed default user if not exists or update role
    var adminUser = db.Users.FirstOrDefault(u => u.Username == "admin");
    if (adminUser == null)
    {
        db.Users.Add(new User { Username = "admin", Password = "user", Role = "Admin" });
        db.SaveChanges();
    }
    else if (adminUser.Role != "Admin")
    {
        adminUser.Role = "Admin";
        db.SaveChanges();
    }
    // Seed standard user
    if (!db.Users.Any(u => u.Username == "user"))
    {
        db.Users.Add(new User { Username = "user", Password = "user", Role = "User" });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom route for SAS Fun Day landing page
app.MapControllerRoute(
    name: "sasfunday",
    pattern: "25JunSchoolFunDay",
    defaults: new { controller = "Invitations", action = "SasFunDay" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
