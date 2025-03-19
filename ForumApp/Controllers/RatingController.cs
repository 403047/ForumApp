using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ForumApp.Controllers
{
    public class RatingController : Controller
    {
        private readonly ForumDbContext _context;

        public RatingController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RatePost(int postId, int rating)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá!" });
            }

            // Kiểm tra xem người dùng đã đánh giá bài viết này chưa
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

            if (existingRating != null)
            {
                return Json(new { success = false, message = "Bạn đã đánh giá bài viết này rồi!" });
            }

            // Lưu đánh giá mới
            var newRating = new Rating
            {
                PostId = postId,
                UserId = userId.Value,
                Stars = rating
            };

            _context.Ratings.Add(newRating);
            await _context.SaveChangesAsync();

            // Cập nhật tổng đánh giá của bài viết
            await UpdatePostTotalRating(postId);

            return Json(new { success = true, message = "Đánh giá thành công!" });
        }

        private async Task UpdatePostTotalRating(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return;

            var ratings = await _context.Ratings
                .Where(r => r.PostId == postId)
                .Select(r => r.Stars)
                .ToListAsync();

            post.TotalRating = ratings.Any() ? (decimal?)Convert.ToDecimal(ratings.Average()) : null;

            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }
    }
}
