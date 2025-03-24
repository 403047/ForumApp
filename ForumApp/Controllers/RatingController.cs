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

            // Thay vì gán post.TotalRating (read-only), ta chỉ tính trung bình tại chỗ
            // để trả về cho Ajax (hoặc hiển thị ngay nếu cần).
            var ratings = await _context.Ratings
                .Where(r => r.PostId == postId)
                .Select(r => r.Stars)
                .ToListAsync();

            var averageRating = ratings.Any() ? ratings.Average() : 0;

            // Trả về JSON để JavaScript có thể hiển thị ngay.
            return Json(new
            {
                success = true,
                message = "Đánh giá thành công!",
                average = averageRating.ToString("0.0")
            });
        }
    }
}
