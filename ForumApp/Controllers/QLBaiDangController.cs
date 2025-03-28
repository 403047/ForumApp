using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApp.Data;
using ForumApp.Models;
using System.Linq;

namespace ForumApp.Controllers
{
    public class QLBaiDangController : Controller
    {
        private readonly ForumDbContext _context;

        public QLBaiDangController(ForumDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var pendingPosts = _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostPrices)
                .Where(p => p.Status == "Chờ duyệt")
                .ToList();

            return View("QLBaiDang", pendingPosts);
        }

        public IActionResult ChiTietBaiDang(int id)
        {
            var post = _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostPrices)
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View("ChiTietBaiDang", post);
        }

        [HttpPost]
        public IActionResult DuyetBai(int id)
        {
            var post = _context.Posts.Find(id);
            if (post == null)
            {
                return NotFound();
            }

            post.Status = "Đã duyệt";
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult KhongDuyetBai(int id)
        {
            var post = _context.Posts.Find(id);
            if (post == null)
            {
                return NotFound();
            }

            post.Status = "Duyệt thất bại";
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
