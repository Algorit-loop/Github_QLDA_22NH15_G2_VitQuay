using Microsoft.AspNetCore.Mvc;
using AppEL.ViewModels;
using AppEL.Services;

namespace AppEL.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly JsonFileService _jsonFileService;

        public AccountController(AccountService accountService, ILogger<AccountController> logger, JsonFileService jsonFileService)
        {
            _accountService = accountService;
            _logger = logger;
            _jsonFileService = jsonFileService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _accountService.LoginAsync(model.Username, model.Password);
                    if (result)
                    {
                        _logger.LogInformation("User {Username} logged in successfully", model.Username);
                        return RedirectToAction("Index", "Home");
                    }
                    _logger.LogWarning("Failed login attempt for user {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng nhập");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _accountService.RegisterAsync(
                        model.Username,
                        model.Email,
                        model.Password,
                        model.FullName);

                    if (result)
                    {
                        _logger.LogInformation("User {Username} registered successfully", model.Username);
                        TempData["SuccessMessage"] = "Đăng ký thành công! Bạn đã được tự động đăng nhập.";
                        return RedirectToAction("Index", "Home");
                    }
                    
                    _logger.LogWarning("Registration failed for user {Username} - Username or email already exists", model.Username);
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc email đã tồn tại");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình đăng ký");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");
            var user = _jsonFileService.GetUsers().FirstOrDefault(u => u.Username == userId);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(decimal amount)
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");
            
            var user = _jsonFileService.GetUsers().FirstOrDefault(u => u.Username == userId);
            if (user == null) return RedirectToAction("Login");
            
            if (amount > 0 && _jsonFileService.DepositToUser(user.Id, amount))
            {
                // Re-login to update the balance claim
                await _accountService.LoginAsync(user.Username, user.Password);
                TempData["SuccessMessage"] = $"Nạp {amount:N0} VNĐ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Nạp tiền thất bại.";
            }
            return RedirectToAction("Deposit");
        }
    }
}
