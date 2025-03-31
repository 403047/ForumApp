using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ForumApp.Controllers
{
    [Route("Bookmark")]
    public class BookMarkController : Controller
    {
        private readonly ForumDbContext _context;

        public BookMarkController(ForumDbContext context)
        {
            _context = context;
        }

        // Action ToggleBookmark có route là "/Bookmark/AddOrRemove"
        [HttpPost("AddOrRemove")]
        public async Task<IActionResult> ToggleBookmark(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var existingBookmark = await _context.BookMarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);

            if (existingBookmark != null)
            {
                _context.BookMarks.Remove(existingBookmark);
                await _context.SaveChangesAsync();
                return Json(new { success = true, bookmarked = false, message = "Đã xóa khỏi Bookmark!" });
            }
            else
            {
                var newBookmark = new BookMark { UserId = userId.Value, PostId = postId };
                _context.BookMarks.Add(newBookmark);
                await _context.SaveChangesAsync();
                return Json(new { success = true, bookmarked = true, message = "Đã thêm vào Bookmark!" });
            }
        }

        // Action GetBookmarkedPosts trả về danh sách bài viết đã bookmark dưới dạng JSON
        [HttpGet]
        public async Task<IActionResult> GetBookmarkedPosts()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var bookmarkedPosts = await _context.BookMarks
                .Where(b => b.UserId == userId)
                .Include(b => b.Post)
                    .ThenInclude(p => p.Category)
                .Include(b => b.Post)
                    .ThenInclude(p => p.User)
                .Select(b => new
                {
                    Id = b.Post.Id,
                    Title = b.Post.Title,
                    Username = b.Post.User != null ? b.Post.User.Username : "Không có người dùng",
                    CreatedAt = b.Post.CreatedAt.ToString("dd/MM/yyyy"),
                    TotalRating = b.Post.TotalRating.ToString("0.0"),
                    Price = b.Post.CurrentPrice.HasValue ? b.Post.CurrentPrice.Value.ToString("0.00") + " VNĐ" : "Miễn phí",
                    CategoryName = b.Post.Category != null ? b.Post.Category.Name : "Không có danh mục"
                })
                .ToListAsync();

            return Json(bookmarkedPosts);
        }

        // Action Index trả về view "BookMark" với danh sách bài viết đã bookmark của người dùng
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var bookmarkedPosts = await _context.BookMarks
                .Where(b => b.UserId == userId)
                .Include(b => b.Post)
                    .ThenInclude(p => p.Category)
                .Select(b => b.Post)
                .ToListAsync();

            return View("BookMark", bookmarkedPosts);
        }
    }
}
