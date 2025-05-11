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

        // Kiểm tra danh mục trước khi load Index.
        public IActionResult Index()
        {
            if (!_context.Categories.Any())
            {
                TempData["ErrorMessage"] = "Chưa có danh mục. Vui lòng thêm danh mục trước!";
                return RedirectToAction("Index", "Category");
            }
            return View();
        }

        // Action trả về danh sách danh mục dưới dạng JSON (không thay đổi)
        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _context.Categories
                .Select(c => new { id = c.Id, name = c.Name })
                .ToList();
            return Json(categories);
        }

        // Action trả về danh sách bài viết đã được duyệt dưới dạng JSON, tính giá dựa trên PostPrice
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

            var postDtos = posts.Select(p => new
            {
                p.Id,
                p.Title,
                type = p.Type != null ? p.Type.Trim() : "",
                userId = p.User != null ? p.User.Id : 0,
                Username = p.User != null ? p.User.Username : "Không xác định",
                CreatedAt = p.CreatedAt.ToString("dd/MM/yyyy"),
                TotalRating = p.TotalRating.ToString("0.0"),
                Price = (p.Type != null && p.Type.Trim().ToLower() == "trả phí" && p.PostPrices.Any())
                     ? (p.PostPrices.OrderByDescending(pp => pp.Id).First().Price > 0
                     ? string.Format("{0:N0}", p.PostPrices.OrderByDescending(pp => pp.Id).First().Price) + " VNĐ"
                     : "Chưa có giá")
                     : "Miễn phí",

                CategoryId = p.Category != null ? p.Category.Id.ToString() : ""
            });

            return Json(postDtos);
        }

        // Action hiển thị chi tiết bài viết
        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var post = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.PostPrices)
                .Include(p => p.Ratings)
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            decimal? gia = null;
            if (post.Type != null && post.Type.Trim().ToLower() == "trả phí" && post.PostPrices.Any())
            {
                gia = post.PostPrices.OrderByDescending(pp => pp.Id).First().Price;
            }
            ViewBag.Price = gia ?? 0;

            bool isBookmarked = false;
            if (userId.HasValue)
            {
                isBookmarked = _context.BookMarks.Any(b => b.UserId == userId && b.PostId == id);
            }
            ViewBag.IsBookmarked = isBookmarked;

            // Đếm số trang file Word nếu có file
            if (!string.IsNullOrEmpty(post.FilePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        var doc = new Aspose.Words.Document(filePath);
                        post.TotalPages = doc.PageCount;
                    }
                    catch
                    {
                        post.TotalPages = 1;
                    }
                }
                else
                {
                    post.TotalPages = 1;
                }
            }
            else
            {
                post.TotalPages = 1;
            }

            return View(post);
        }

        // Action hiển thị nội dung file Word theo trang
        [HttpGet]
        public async Task<IActionResult> ViewWordContent(int id, int pageNumber = 1)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null || string.IsNullOrEmpty(post.FilePath))
                return NotFound("Bài viết không tồn tại hoặc không có file Word.");

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound("File Word không tồn tại trên server.");

            try
            {
                Document doc = new Document(filePath);
                if (pageNumber < 1)
                    pageNumber = 1;
                if (pageNumber > doc.PageCount)
                    pageNumber = doc.PageCount;

                int pageIndex = pageNumber - 1;
                Document singlePageDoc = doc.ExtractPages(pageIndex, 1);

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

            // Kiểm tra nếu là bài viết trả phí thì người dùng phải có mã QR
            if (!string.IsNullOrWhiteSpace(post.Type) && post.Type.Trim().ToLower() == "trả phí")
            {
                // Kiểm tra giá tiền âm
                if (!Price.HasValue || Price.Value < 0)
                {
                    ModelState.AddModelError("", "Giá tiền không thể âm");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View(post);
                }
                if (string.IsNullOrWhiteSpace(user.QRImagePath))
                {
                    ModelState.AddModelError("", "Người dùng chưa có mã QR.");
                    ViewBag.Categories = _context.Categories.ToList();
                    return View(post);
                }
            }

            post.UserId = user.Id;
            post.CreatedAt = DateTime.Now;
            post.Status = "Chờ duyệt";

            // Nếu người dùng upload file
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
            else // Nếu người dùng nhập nội dung qua trình soạn thảo
            {
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
                        builder.InsertHtml(contentText); // dùng InsertHtml để giữ định dạng nếu có
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

                // Nếu là bài viết trả phí thì lưu giá
                if (!string.IsNullOrWhiteSpace(post.Type) && post.Type.Trim().ToLower() == "trả phí" && Price.HasValue)
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

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
                return Json(new { uploaded = false, error = new { message = "Không có file ảnh." } });

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(upload.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await upload.CopyToAsync(stream);
            }

            var url = "/uploads/images/" + fileName;
            return Json(new { uploaded = true, url });
        }
        [HttpPost]
        public async Task<IActionResult> Rate(int postId, int stars)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá!" });
            }

            if (stars < 1 || stars > 5)
            {
                return Json(new { success = false, message = "Số sao không hợp lệ!" });
            }

            try
            {
                // Kiểm tra đã từng đánh giá chưa
                var existing = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId.Value);
               
                if (existing != null)
                {
                    existing.Stars = stars;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var rating = new Rating
                    {
                        PostId = postId,
                        UserId = userId.Value,
                        Stars = stars
                    };
                    _context.Ratings.Add(rating);
                    await _context.SaveChangesAsync();
                }
                // Tính lại tổng đánh giá
                var ratings = await _context.Ratings.Where(r => r.PostId == postId).ToListAsync();
                double totalRating = ratings.Any() ? ratings.Average(r => r.Stars) : 0;
                return Json(new { success = true, message = "Đánh giá thành công!", totalRating = totalRating });
            }
            catch (Exception ex)
            {
                // Ghi log nếu cần
                return Json(new { success = false, message = "Đã xảy ra lỗi khi gửi đánh giá." });
            }
        }
        // ACTION ThanhToan: Tạo hóa đơn và chi tiết hóa đơn
        // Controllers/PostController.cs
        [HttpPost]
        public async Task<IActionResult> ThanhToan(int postId, IFormFile minhChung)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thanh toán." });
            }

            var post = await _context.Posts
                .Include(p => p.PostPrices)
                .Include(p => p.User)             // cần để lấy QRImagePath
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null || post.Type.Trim().ToLower() != "trả phí")
            {
                return Json(new { success = false, message = "Bài viết không hợp lệ hoặc không cần thanh toán." });
            }

            // Nếu là tác giả, chuyển luôn sang Details mà không tạo/update hóa đơn
            if (post.UserId == userId)
            {
                return RedirectToAction("Details", new { id = postId });
            }

            if (minhChung == null || minhChung.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng tải lên ảnh minh chứng thanh toán." });
            }

            // Lưu ảnh minh chứng
            string minhChungPath;
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "minhchung");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(minhChung.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await minhChung.CopyToAsync(stream);

                minhChungPath = "/minhchung/" + fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu ảnh minh chứng.");
                return Json(new { success = false, message = "Lỗi khi lưu ảnh minh chứng." });
            }

            try
            {
                // Kiểm tra ChiTietHoaDon đang chờ xác thực
                var existing = await _context.ChiTietHoaDons
                    .Include(ct => ct.HoaDon)
                    .Where(ct => ct.IdUser == userId && ct.IdPost == postId && ct.HoaDon.Status == "Chờ xác thực")
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Cập nhật lại minh chứng và QR
                    existing.MinhChung = minhChungPath;
                    existing.QRImagePath = post.User.QRImagePath;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Cập nhật ảnh minh chứng thành công. Đang chờ xác thực." });
                }
                else
                {
                    // Tạo mới HoaDon
                    var hoaDon = new HoaDon
                    {
                        Status = "Chờ xác thực",
                        AuthenticationTimes = 0
                    };
                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    // Lấy giá mới nhất
                    var gia = post.PostPrices
                                .OrderByDescending(pp => pp.Id)
                                .FirstOrDefault()?.Price ?? 0;

                    // Tạo ChiTietHoaDon mới với cả MinhChung và QR của chủ bài
                    var chiTiet = new ChiTietHoaDon
                    {
                        IdHoaDon = hoaDon.Id,
                        IdPost = post.Id,
                        IdUser = userId.Value,
                        Gia = gia,
                        MinhChung = minhChungPath,
                        QRImagePath = post.User.QRImagePath
                    };
                    _context.ChiTietHoaDons.Add(chiTiet);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Gửi thanh toán thành công. Đang chờ xác thực." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu hóa đơn.");
                return Json(new { success = false, message = "Lỗi khi tạo hóa đơn. Vui lòng thử lại." });
            }
        }


        // ACTION CheckPaymentStatus: Kiểm tra trạng thái thanh toán của bài viết
        [HttpPost]
        public async Task<IActionResult> CheckPaymentStatus(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { status = "", price = "", qr = "" });

            var post = await _context.Posts
                .Include(p => p.PostPrices)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return Json(new { status = "", price = "", qr = "" });

            string priceString = (post.Type != null && post.Type.Trim().ToLower() == "trả phí" && post.CurrentPrice.HasValue && post.CurrentPrice.Value > 0)
                ? post.CurrentPrice.Value.ToString("N0") + " VNĐ"
                : "Chưa có giá";
            string qrImage = post.User?.QRImagePath ?? "";

            // Nếu người dùng là tác giả, coi như đã trả phí
            if (post.UserId == userId)
            {
                return Json(new { status = "Đã xác thực", price = priceString, qr = qrImage });
            }

            var chiTiet = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.IdUser == userId && ct.IdPost == postId)
                .FirstOrDefaultAsync();

            if (chiTiet != null)
            {
                if (chiTiet.HoaDon.Status == "Đã xác thực")
                {
                    return Json(new { status = "Đã xác thực", price = priceString, qr = qrImage });
                }
                else
                {
                    return Json(new { status = chiTiet.HoaDon.Status, price = priceString, minhChung = chiTiet.MinhChung, qr = qrImage });
                }
            }
            else
            {
                return Json(new { status = "Chưa thanh toán", price = priceString, qr = qrImage });
            }
        }
    }
}
