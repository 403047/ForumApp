using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApp.Data;
using ForumApp.Models;
using System;
using System.Threading.Tasks;

namespace ForumApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ForumDbContext _context;

        public AccountController(ForumDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // Lưu thông tin vào Session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserRole", user.Role.RoleName);

                // Kiểm tra session ngay sau khi thiết lập
                int? sessionUserId = HttpContext.Session.GetInt32("UserId");
                string sessionUserRole = HttpContext.Session.GetString("UserRole");

                Console.WriteLine($"Session UserId: {sessionUserId}, Session UserRole: {sessionUserRole}");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Thông tin đăng nhập không hợp lệ";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            Console.WriteLine($"Đang đăng ký với Username: {username}, Email: {email}");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // Kiểm tra username hoặc email đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc email đã tồn tại.";
                return View();
            }

            // Mã hóa mật khẩu
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Tạo user mới với IdRole mặc định là 1
            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = hashedPassword,
                IdRole = 1 // Gán cố định IdRole là 1
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                Console.WriteLine("Đăng ký thành công!");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đăng ký: {ex.Message}");
                ViewBag.Error = "Có lỗi xảy ra khi đăng ký.";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
