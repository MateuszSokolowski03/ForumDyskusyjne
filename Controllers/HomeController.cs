using Microsoft.AspNetCore.Mvc;

namespace ForumDyskusyjne.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return Redirect("/index.html");
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return Redirect("/login.html");
        }
        
        [HttpGet("register")]
        public IActionResult Register()
        {
            return Redirect("/register.html");
        }

        [HttpGet("forum")]
        public IActionResult Forum()
        {
            return Redirect("/forum.html");
        }
        
        [HttpGet("admin")]
        public async Task<IActionResult> Admin()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "admin", "index.html");
            if (System.IO.File.Exists(filePath))
            {
                Response.ContentType = "text/html";
                await Response.SendFileAsync(filePath);
                return new EmptyResult();
            }
            return NotFound();
        }
        
        [HttpGet("admin/{page}")]
        public async Task<IActionResult> AdminPage(string page)
        {
            if (page.Contains("..") || page.Contains("/") || page.Contains("\\"))
            {
                return BadRequest("Invalid page name");
            }
            
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "admin", $"{page}.html");
            if (System.IO.File.Exists(filePath))
            {
                Response.ContentType = "text/html";
                await Response.SendFileAsync(filePath);
                return new EmptyResult();
            }
            return NotFound($"Admin page '{page}' not found");
        }

        [HttpGet("admin/users")]
        public IActionResult AdminUsers()
        {
            return Redirect("/admin/users.html");
        }

        [HttpGet("admin/categories")]
        public IActionResult AdminCategories()
        {
            return Redirect("/admin/categories.html");
        }

        [HttpGet("admin/threads")]
        public IActionResult AdminThreads()
        {
            return Redirect("/admin/threads.html");
        }

        [HttpGet("admin/banned-words")]
        public IActionResult AdminBannedWords()
        {
            return Redirect("/admin/banned-words.html");
        }

        [HttpGet("admin/settings")]
        public IActionResult AdminSettings()
        {
            return Redirect("/admin/settings.html");
        }
    }
}