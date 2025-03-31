using Microsoft.AspNetCore.Mvc;
using ForumApp.Data;
using ForumApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ForumApp.Controllers
{
    public class SuaThongTinController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SuaThongTinController(ForumDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: SuaThongTin/Index
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            // Trả về view có tên "SuaThongTin.cshtml" thay vì "Index.cshtml"
            return View("SuaThongTin", user);
        }

        // POST: SuaThongTin/Update
        [HttpPost]
        public async Task<IActionResult> Update(User model, IFormFile qrImage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật email
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                user.Email = model.Email;
            }

            // Cập nhật ảnh QR nếu có file mới
            if (qrImage != null && qrImage.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Xóa ảnh QR cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(user.QRImagePath))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.QRImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(qrImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await qrImage.CopyToAsync(stream);
                    }
                    user.QRImagePath = "/uploads/" + fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi tải ảnh QR: " + ex.Message);
                    return View("SuaThongTin", user);
                }
            }

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin của bạn đã được cập nhật thành công!";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật: " + ex.Message);
                return View("SuaThongTin", user);
            }

            return RedirectToAction("Index");
        }

        // POST: SuaThongTin/ChangePassword
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng.");
                return View("SuaThongTin", user);
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới không khớp.");
                return View("SuaThongTin", user);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
    }
}
