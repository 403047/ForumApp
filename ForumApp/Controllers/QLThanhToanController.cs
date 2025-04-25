using System;
using System.Linq;
using System.Threading.Tasks;
using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForumApp.Controllers
{
    public class QLThanhToanController : Controller
    {
        private readonly ForumDbContext _context;
        public QLThanhToanController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: hiển thị danh sách các hóa đơn cần quản lý
        public async Task<IActionResult> Index()
        {
            var cutoff = DateTime.Now.AddDays(-1);
            var list = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Include(ct => ct.Post)
                .Include(ct => ct.User)
                .Where(ct =>
                    ct.HoaDon.AuthenticationTimes >= 1
                    || ct.HoaDon.DateSend <= cutoff
                )
                .Select(ct => new QLThanhToanViewModel
                {
                    ChiTietId = ct.Id,
                    PostId = ct.IdPost,
                    PostTitle = ct.Post.Title,
                    UserId = ct.IdUser,
                    Username = ct.User.Username,
                    Gia = ct.Gia,
                    MinhChung = ct.MinhChung,
                    QRImagePath = ct.QRImagePath,
                    Status = ct.HoaDon.Status,
                    AuthenticationTimes = ct.HoaDon.AuthenticationTimes,
                    DateSend = ct.HoaDon.DateSend
                })
                .ToListAsync();

            return View(list);
        }

        // POST: xác thực hóa đơn (tăng AuthenticationTimes lên 2)
        [HttpPost]
        public async Task<IActionResult> Approve(int chiTietId)
        {
            var ct = await _context.ChiTietHoaDons
                        .Include(x => x.HoaDon)
                        .FirstOrDefaultAsync(x => x.Id == chiTietId);
            if (ct == null) return Json(new { success = false, message = "Không tìm thấy hóa đơn." });

            ct.HoaDon.Status = "Đã xác thực";
            ct.HoaDon.AuthenticationTimes = 2;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: từ chối hóa đơn
        [HttpPost]
        public async Task<IActionResult> Reject(int chiTietId)
        {
            var ct = await _context.ChiTietHoaDons
                        .Include(x => x.HoaDon)
                        .FirstOrDefaultAsync(x => x.Id == chiTietId);
            if (ct == null) return Json(new { success = false, message = "Không tìm thấy hóa đơn." });

            ct.HoaDon.Status = "Xác thực thất bại";
            ct.HoaDon.AuthenticationTimes = 2;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // ViewModel để đẩy lên view
    public class QLThanhToanViewModel
    {
        public int ChiTietId { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public decimal Gia { get; set; }
        public string MinhChung { get; set; }
        public string QRImagePath { get; set; }
        public string Status { get; set; }
        public int AuthenticationTimes { get; set; }
        public DateTime DateSend { get; set; }
    }
}
