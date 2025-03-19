using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ForumApp.Data;
using ForumApp.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Words;

namespace ForumApp.Controllers
{
    public class PostController : Controller
    {
        private readonly ForumDbContext _context;

        public PostController(ForumDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách bài viết
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.Include(p => p.Category).ToListAsync();
            return View(posts);
        }

        // Hiển thị form tạo bài viết
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();

            return View();
        }

        // Xử lý tạo bài viết
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync();

                return View(post);
            }

            // Kiểm tra người dùng đã nhập nội dung hay chọn file
            bool hasContent = !string.IsNullOrEmpty(post.Content);
            bool hasFile = file != null && file.Length > 0;

            if (hasContent && hasFile)
            {
                ModelState.AddModelError("", "Chỉ có thể nhập nội dung hoặc tải lên file, không thể chọn cả hai.");
                ViewBag.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToListAsync();

                return View(post);
            }

            if (hasFile)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Đọc nội dung file Word và lưu vào database
                post.Content = ExtractTextFromWord(filePath);
                post.FilePath = "/uploads/" + uniqueFileName;
            }

            post.CreatedAt = DateTime.Now;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Hàm đọc nội dung file Word (.doc, .docx)
        private string ExtractTextFromWord(string filePath)
        {
            try
            {
                Document doc = new Document(filePath);
                return doc.ToString(SaveFormat.Text);
            }
            catch (Exception)
            {
                return "Không thể đọc nội dung file Word.";
            }
        }
    }
}
