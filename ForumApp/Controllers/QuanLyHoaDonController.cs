using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForumApp.Controllers
{
    public class QuanLyHoaDonController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly IWebHostEnvironment _env;
        public QuanLyHoaDonController(ForumDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /QuanLyHoaDon
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var items = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Include(ct => ct.Post)
                .Where(ct => ct.IdUser == userId)
                .OrderByDescending(ct => ct.Id)
                .ToListAsync();

            return View(items);
        }

        // GET: /QuanLyHoaDon/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(ctt => ctt.HoaDon)
                .FirstOrDefaultAsync(ctt => ctt.Id == id);
            if (ct == null || ct.HoaDon.Status != "Chờ xác thực")
                return RedirectToAction("Index");

            return View(ct);
        }

        // POST: /QuanLyHoaDon/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, IFormFile minhChung)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(c => c.HoaDon)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (ct == null || minhChung == null || minhChung.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn ảnh minh chứng.");
                return View(ct);
            }

            // Lưu file mới
            var uploads = Path.Combine(_env.WebRootPath, "minhchung");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(minhChung.FileName);
            var path = Path.Combine(uploads, fileName);
            using (var fs = new FileStream(path, FileMode.Create))
                await minhChung.CopyToAsync(fs);

            ct.MinhChung = "/minhchung/" + fileName;
            // (Không tăng AuthenticationTimes ở đây)
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật minh chứng thành công.";
            return RedirectToAction("Index");
        }

        // POST: /QuanLyHoaDon/Delete/5
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(c => c.HoaDon)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (ct == null || !(ct.HoaDon.Status == "Chờ xác thực" || ct.HoaDon.Status == "Xác thực thất bại"))
                return Json(new { success = false, message = "Không thể xóa hóa đơn này." });

            // Xóa Chi tiết và bản thân HoaDon
            _context.ChiTietHoaDons.Remove(ct);
            _context.HoaDons.Remove(ct.HoaDon);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: /QuanLyHoaDon/Resend/5
        [HttpPost]
        public async Task<JsonResult> Resend(int id)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(c => c.HoaDon)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (ct == null || ct.HoaDon.Status != "Xác thực thất bại")
                return Json(new { success = false, message = "Không thể gửi lại hóa đơn này." });

            ct.HoaDon.Status = "Chờ xác thực";
            ct.HoaDon.AuthenticationTimes += 1;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
