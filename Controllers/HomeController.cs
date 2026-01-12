using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForumDyskusyjne.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
         [AllowAnonymous]
        [HttpGet("")]
        public IActionResult Index()
        {
            return Redirect("/index.html");
        }
        
         [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login()
        {
            return Redirect("/login.html");
        }
        [AllowAnonymous]    
        [HttpGet("register")]
        public IActionResult Register()
        {
            return Redirect("/register.html");
        }
        [AllowAnonymous]
        [HttpGet("forum")]
        public async Task<IActionResult> Forum()
        {
            // Sprawdź czy użytkownik jest zalogowany
            if (HttpContext.Request.Cookies.ContainsKey("user_session"))
            {
                var sessionValue = HttpContext.Request.Cookies["user_session"];
                if (!string.IsNullOrEmpty(sessionValue))
                {
                    // Zalogowany - zwróć forum.html z sesją
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "forum.html");
                    if (System.IO.File.Exists(filePath))
                    {
                        Response.ContentType = "text/html";
                        await Response.SendFileAsync(filePath);
                        return new EmptyResult();
                    }
                }
            }
            
            // Nie zalogowany - redirect na login
            return Redirect("/login");
        }
        [AllowAnonymous]
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
        [AllowAnonymous]
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
        [AllowAnonymous]
        [HttpGet("admin/users")]
        public IActionResult AdminUsers()
        {
            return Redirect("/admin/users.html");
        }
        [AllowAnonymous]
        [HttpGet("admin/categories")]
        public IActionResult AdminCategories()
        {
            return Redirect("/admin/categories.html");
        }
        [AllowAnonymous]
        [HttpGet("admin/threads")]
        public IActionResult AdminThreads()
        {
            return Redirect("/admin/threads.html");
        }
        [AllowAnonymous]
        [HttpGet("admin/banned-words")]
        public IActionResult AdminBannedWords()
        {
            return Redirect("/admin/banned-words.html");
        }
    }
}