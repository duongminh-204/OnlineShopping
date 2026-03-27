using ASP.BaseCommon;
using ASP.Hubs;
using ASP.Models;
using ASP.Models.Admin.Roles;
using ASP.Models.Admin.ThemeOptions;
using ASP.Models.ASPModel;
using ASP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Spire.Xls;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace ASP.Controllers.Front
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public ThemeOptionRepositoryInterface _themeOption;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ASPDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuthorizationService _authService;
        public BaseController _baseController;

        public HomeController(ASPDbContext context, ILogger<HomeController> logger, ThemeOptionRepositoryInterface themeOption, IWebHostEnvironment webHostEnvironment, IAuthorizationService authService, IHubContext<NotificationHub> hubContext, BaseController baseController)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _themeOption = themeOption;
            _webHostEnvironment = webHostEnvironment;
            _authService = authService;
            _baseController = baseController;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories
                .OrderBy(c => c.CategoryName)
                .Select(c => new HomeCategoryItemViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ImageUrl = _context.Products
                        .Where(p => p.CategoryId == c.CategoryId && p.IsActive)
                        .Where(p => p.ProductVariants.Any(v => v.IsActive))
                        .OrderByDescending(p => p.ProductId)
                        .Select(p => p.ProductImages
                            .OrderByDescending(img => img.IsMain)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault())
                        .FirstOrDefault() ?? "/images/no-image.jpg"
                })
                .ToList();

            var baseQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive)
                .Where(p => p.ProductVariants.Any(v => v.IsActive));

            var popularProducts = baseQuery
                .OrderByDescending(p => _context.OrderDetails
                    .Where(od => od.ProductVariant.ProductId == p.ProductId)
                    .Sum(od => (int?)od.Quantity) ?? 0)
                .Take(8)
                .ToList();

            var newestProducts = baseQuery
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .ToList();

            var viewModel = new HomeViewModel
            {
                Categories = categories,
                PopularProducts = popularProducts,
                NewestProducts = newestProducts
            };

            return View("../Front/Home/Index", viewModel);
        }

        [Route("notification", Name = "front.home.Notify")]
        public async Task<IActionResult> Notify(string message)
        {
            var cLog = _context.Logs.Count();
            await _hubContext.Clients.All.SendAsync("Notify", $"{cLog}");
            return View("../Front/Home/Notification");
        }

        public IActionResult Privacy()
        {
            return View("../Front/Home/Privacy");
        }

        public async Task<IActionResult> Guide()
        {
            var obj = _themeOption.GetThemeOption("_optguide", "home_tab");
            ViewBag.vbGuide = "";
            if (obj != null)
            {
                ViewBag.vbGuide = obj.Value;
            }
            return View("../Front/Home/Guide");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}