﻿using Microsoft.AspNetCore.Mvc;
using ForumApp.Models;
using ForumApp.Data;
using System.Linq;

namespace ForumApp.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly ForumDbContext _context;

        public TaiKhoanController(ForumDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string searchTerm)
        {
            var users = _context.Users
                .Join(_context.Roles,
                    u => u.IdRole,
                    r => r.IdRole,
                    (u, r) => new {
                        u.Id,
                        u.Username,
                        RoleName = r.RoleName,
                        Status = u.Status ? "Active" : "Inactive"
                    })
                .Where(u => string.IsNullOrEmpty(searchTerm) || u.Username.Contains(searchTerm))
                .ToList();

            return View("QLTaiKhoan", users);
        }

        [HttpPost]
        public IActionResult UpdateUser([FromBody] User model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var user = _context.Users.Find(model.Id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Người dùng không tồn tại" });
            }

            user.IdRole = model.IdRole;
            user.Status = model.Status;
            _context.SaveChanges();

            return Json(new { success = true, message = "Cập nhật thành công" });
        }
    }
}
