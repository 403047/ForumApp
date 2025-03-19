using Microsoft.EntityFrameworkCore;
using ForumApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Thêm d?ch v? Controller v?i Views
builder.Services.AddControllersWithViews();

// C?u hình k?t n?i SQL Server t? appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging() // Hi?n th? d? li?u trong truy v?n SQL
           .LogTo(Console.WriteLine, LogLevel.Information)); // Ghi log ra console

// Thêm d?ch v? Session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ??m b?o ch? g?i cookie qua HTTPS
    options.Cookie.SameSite = SameSiteMode.None; // Cho phép cookie ho?t ??ng trong nhi?u tên mi?n
});

var app = builder.Build();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None
});

// C?u hình pipeline x? lý HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kích ho?t Session tr??c khi Authorization
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
