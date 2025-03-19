using Microsoft.EntityFrameworkCore;
using ForumApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Th�m d?ch v? Controller v?i Views
builder.Services.AddControllersWithViews();

// C?u h�nh k?t n?i SQL Server t? appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseSqlServer(connectionString));

// Th�m d?ch v? Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Th?i gian h?t h?n Session
    options.Cookie.HttpOnly = true; // Ch? truy c?p Session qua HTTP
    options.Cookie.IsEssential = true; // Cookie Session lu�n ???c s? d?ng
});

var app = builder.Build();

// C?u h�nh pipeline x? l� HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// K�ch ho?t Session tr??c khi Authorization
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
