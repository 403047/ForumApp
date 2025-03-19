using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Words;
using ForumApp.Data;
using ForumApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForumApp.Controllers
{
    public class PostController : Controller
    {
        private readonly ForumDbContext _context;
        private readonly ILogger<PostController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostController(ForumDbContext context, ILogger<PostController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Ratings) // Lấy danh sách đánh giá
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.User,
                    p.CreatedAt,
                    p.Category,
                    TotalRating = p.Ratings.Any() ? (decimal?)p.Ratings.Average(r => r.Stars) : null
                })
                .ToListAsync();

            return View("~/Views/Home/Index.cshtml", posts);
        }


        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Post post, IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "Người dùng không tồn tại.");
                return View(post);
            }

            post.UserId = user.Id;
            post.CreatedAt = DateTime.Now;
            post.Status = "Chờ duyệt";

            if (file != null && file.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    post.FilePath = "/uploads/" + fileName;
                    post.Content = ExtractTextFromWord(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu file: {Message}", ex.Message);
                    ModelState.AddModelError("", "Lỗi khi xử lý file Word.");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View(post);
                }
            }

            try
            {
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bài viết {Title} của người dùng {UserId} đã được lưu thành công.", post.Title, user.Id);
                // Chuyển hướng về trang chủ (Home/Index)
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Lỗi khi cập nhật database: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
                ModelState.AddModelError("", "Lỗi khi lưu bài viết vào database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi lưu bài viết.");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu bài viết. Vui lòng thử lại.");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(post);
        }

        private string ExtractTextFromWord(string filePath)
        {
            try
            {
                var doc = new Document(filePath);
                return doc.ToString(SaveFormat.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất nội dung từ file Word.");
                return "";
            }
        }
    }
}
