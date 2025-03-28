using Microsoft.AspNetCore.Mvc;
using ForumApp.Models;
using ForumApp.Data;
using System.Linq;

namespace ForumApp.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly ForumDbContext _context;

        public DanhMucController(ForumDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var danhMucs = _context.Categories.ToList();
            return View("QLDanhMuc", danhMucs);
        }

        [HttpPost]
        public IActionResult ThemDanhMuc([FromBody] Category model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { success = false, message = "Tên danh mục không hợp lệ" });
            }

            _context.Categories.Add(model);
            _context.SaveChanges();

            var danhMucs = _context.Categories.ToList();
            return PartialView("_DanhMucTable", danhMucs);
        }

        [HttpPost]
        public IActionResult SuaDanhMuc([FromBody] Category model)
        {
            var danhMuc = _context.Categories.Find(model.Id);
            if (danhMuc == null)
            {
                return NotFound(new { success = false, message = "Danh mục không tồn tại" });
            }

            danhMuc.Name = model.Name;
            _context.SaveChanges();

            return PartialView("_DanhMucTable", _context.Categories.ToList());
        }

        [HttpPost]
        public IActionResult XoaDanhMuc([FromBody] Category model)
        {
            var danhMuc = _context.Categories.Find(model.Id);
            if (danhMuc == null)
            {
                return NotFound(new { success = false, message = "Danh mục không tồn tại" });
            }

            _context.Categories.Remove(danhMuc);
            _context.SaveChanges();

            return PartialView("_DanhMucTable", _context.Categories.ToList());
        }

    }
}
