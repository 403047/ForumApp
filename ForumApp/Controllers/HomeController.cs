using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumApp.Data;

namespace ForumApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ForumDbContext _context;

        public HomeController(ForumDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(posts);
        }
    }
}
