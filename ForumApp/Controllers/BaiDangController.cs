using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ForumApp.Controllers
{
    public class BaiDangController : Controller
    {
        private readonly ForumDbContext _context;

        public BaiDangController(ForumDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách bài đăng của người dùng hiện tại
        public async Task<IActionResult> BaiDangUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var posts = await _context.Posts
                .Include(p => p.PostPrices)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return View(posts);
        }

        // Lấy danh sách các ChiTietHoaDon của 1 post
        [HttpGet]
        public async Task<IActionResult> GetPayments(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var payments = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.IdPost == postId)
                .Select(ct => new
                {
                    ct.Id,
                    ct.Gia,
                    ct.MinhChung,
                    ct.User.QRImagePath,
                    Status = ct.HoaDon.Status,
                    ct.HoaDon.AuthenticationTimes
                })
                .ToListAsync();

            return Json(payments);
        }

        // Xác thực (approve) 1 ChiTietHoaDon
        [HttpPost]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(c => c.HoaDon)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (ct == null) return NotFound();

            ct.HoaDon.Status = "Đã xác thực";
            ct.HoaDon.AuthenticationTimes = 1;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Từ chối (reject) 1 ChiTietHoaDon
        [HttpPost]
        public async Task<IActionResult> RejectPayment(int id)
        {
            var ct = await _context.ChiTietHoaDons
                .Include(c => c.HoaDon)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (ct == null) return NotFound();

            ct.HoaDon.Status = "Xác thực thất bại";
            ct.HoaDon.AuthenticationTimes = 1;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Xoa(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();

            if (post.Status == "Chờ duyệt" || post.Status == "Duyệt thất bại")
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không thể xóa bài đăng." });
        }

        [HttpPost]
        public async Task<IActionResult> DoiTrangThai(int id, string trangThaiMoi)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound();

            if ((post.Status == "Đã duyệt" && trangThaiMoi == "Đã ẩn") ||
                (post.Status == "Đã ẩn" && trangThaiMoi == "Đã duyệt"))
            {
                post.Status = trangThaiMoi;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không thể cập nhật trạng thái." });
        }
    }
}
