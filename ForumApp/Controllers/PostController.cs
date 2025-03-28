using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspose.Words;
using Aspose.Words.Saving;
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

        // View chính: load giao diện rỗng, dữ liệu sẽ được tải qua Ajax
        public IActionResult Index()
        {
            return View();
        }

        // Action trả về danh sách bài viết đã được duyệt dưới dạng JSON
        [HttpGet]
        public async Task<IActionResult> GetApprovedPosts()
        {
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Ratings)
                .Include(p => p.PostPrices)
                .Where(p => p.Status == "Đã duyệt")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Lưu ý: mặc định JSON serializer của ASP.NET Core chuyển sang camelCase nên ở JS cần dùng tên thuộc tính viết thường.
            var postDtos = posts.Select(p => new
            {
                p.Id,
                p.Title,
                Username = p.User != null ? p.User.Username : "Không xác định",
                CreatedAt = p.CreatedAt.ToString("dd/MM/yyyy"),
                TotalRating = p.TotalRating.ToString("0.0"),
                Price = p.CurrentPrice.HasValue ? p.CurrentPrice.Value.ToString("0.00") + " VNĐ" : "Miễn phí"
            });

            return Json(postDtos);
        }

        // Action hiển thị chi tiết bài viết
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Ratings)
                .Include(p => p.PostPrices)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound();

            // Nếu có FilePath, có thể đọc file Word để gán số trang (nếu cần hiển thị theo trang)
            if (!string.IsNullOrEmpty(post.FilePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        Document doc = new Document(filePath);
                        // Giả sử bạn có thêm thuộc tính TotalPages (NotMapped) trong Post.cs
                        post.TotalPages = doc.PageCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi đếm trang Word: {Message}", ex.Message);
                        post.TotalPages = null;
                    }
                }
            }

            return View(post);
        }

        // Action dùng để hiển thị nội dung file Word theo trang (chuyển trang nếu có nhiều trang)
        [HttpGet]
        public async Task<IActionResult> ViewWordContent(int id, int pageNumber = 1)
        {
            // Lấy bài viết theo id
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null || string.IsNullOrEmpty(post.FilePath))
                return NotFound("Bài viết không tồn tại hoặc không có file Word.");

            // Lấy đường dẫn vật lý của file Word
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound("File Word không tồn tại trên server.");

            try
            {
                // Đọc file Word bằng Aspose.Words
                Document doc = new Document(filePath);

                // Giới hạn pageNumber trong khoảng hợp lệ
                if (pageNumber < 1)
                    pageNumber = 1;
                if (pageNumber > doc.PageCount)
                    pageNumber = doc.PageCount;

                // Tách ra 1 Document mới chỉ chứa 1 trang (sử dụng ExtractPages)
                int pageIndex = pageNumber - 1;
                Document singlePageDoc = doc.ExtractPages(pageIndex, 1);

                // Tạo HtmlSaveOptions để xuất nội dung thành HTML
                var saveOptions = new HtmlSaveOptions
                {
                    ExportImagesAsBase64 = true
                };

                using (var ms = new MemoryStream())
                {
                    singlePageDoc.Save(ms, saveOptions);
                    ms.Position = 0;
                    var htmlContent = Encoding.UTF8.GetString(ms.ToArray());
                    return Content(htmlContent, "text/html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý file Word: {Message}", ex.Message);
                return BadRequest("Không thể hiển thị nội dung file Word.");
            }
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Post post, IFormFile file, decimal? Price)
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
            // Cho mục đích demo, đặt status là "Đã duyệt" (thông thường bài viết mới sẽ là "Chờ duyệt")
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu file: {Message}", ex.Message);
                    ModelState.AddModelError("", "Lỗi khi xử lý file Word.");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View(post);
                }
            }
            else
            {
                // Nếu không upload file, kiểm tra nội dung text được gửi qua form (để chuyển thành file Word)
                var contentText = Request.Form["Content"].ToString();
                if (!string.IsNullOrWhiteSpace(contentText))
                {
                    try
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        Document doc = new Document();
                        DocumentBuilder builder = new DocumentBuilder(doc);
                        builder.Write(contentText);

                        var fileName = Guid.NewGuid().ToString() + ".docx";
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        doc.Save(filePath);

                        post.FilePath = "/uploads/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi tạo file Word từ nội dung: {Message}", ex.Message);
                        ModelState.AddModelError("", "Lỗi khi tạo file Word từ nội dung.");
                        ViewBag.Categories = _context.Categories.ToList();
                        return View(post);
                    }
                }
            }

            try
            {
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                if (post.Type == "Trả phí" && Price.HasValue)
                {
                    var postPrice = new PostPrice
                    {
                        PostId = post.Id,
                        Price = Price.Value
                    };
                    _context.PostPrices.Add(postPrice);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Bài viết {Title} của người dùng {UserId} đã được lưu thành công.", post.Title, user.Id);
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
                Document doc = new Document(filePath);
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
