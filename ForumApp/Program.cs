using Microsoft.EntityFrameworkCore;
using ForumApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Thêm d?ch v? Controller v?i Views
builder.Services.AddControllersWithViews();

// C?u hình k?t n?i SQL Server t? appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseSqlServer(connectionString));

// Thêm d?ch v? Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Th?i gian h?t h?n Session
    options.Cookie.HttpOnly = true; // Ch? truy c?p Session qua HTTP
    options.Cookie.IsEssential = true; // Cookie Session luôn ???c s? d?ng
});

var app = builder.Build();

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
