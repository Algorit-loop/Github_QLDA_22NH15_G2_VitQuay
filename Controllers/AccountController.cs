using Microsoft.AspNetCore.Mvc;
using AppEL.ViewModels;
using AppEL.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json; // Added for TempData serialization

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.RegisterAsync(model.Username, model.Password, model.Email, model.FullName, model.Role);
                if (result.Succeeded)
                {
                    // User will be signed in by the service if registration is successful
                    // Or, if you prefer manual sign-in after registration:
                    // var userToSignIn = await _accountService.FindByUsernameAsync(model.Username);
                    // if (userToSignIn != null) await _accountService.SignInAsync(userToSignIn);

                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập hoặc bạn đã được tự động đăng nhập.";
                    // Redirect to login or home depending on whether auto-login is implemented in RegisterAsync/SignInAsync
                    return RedirectToAction("Login"); 
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            return View(model);
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.SendPasswordResetOtpAsync(model.Email);
                
                if (result)
                {
                    _logger.LogInformation("Password reset OTP sent to {Email}", model.Email);
                    ViewData["SuccessMessage"] = "Mã xác nhận đã được gửi đến email của bạn.";
                    
                    return View("VerifyOtp", new VerifyOtpViewModel { Email = model.Email });
                }
                
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", model.Email);
                ModelState.AddModelError("", "Không tìm thấy tài khoản với email này.");
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return Json(new { success = false, message = "Email không hợp lệ" });
            }

            var result = await _accountService.ResendOtpAsync(request.Email);
            
            if (result)
            {
                _logger.LogInformation("Password reset OTP resent to {Email}", request.Email);
                return Json(new { success = true, message = "Mã xác nhận đã được gửi lại thành công." });
            }
            
            return Json(new { success = false, message = "Không thể gửi lại mã xác nhận. Vui lòng thử lại sau." });
        }

        public class ResendOtpRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var isValid = _accountService.VerifyOtp(model.Email, model.OtpCode);
                
                if (isValid)
                {
                    _logger.LogInformation("OTP verification successful for {Email}", model.Email);
                    
                    return View("ResetPassword", new ResetPasswordViewModel 
                    { 
                        Email = model.Email, 
                        OtpCode = model.OtpCode 
                    });
                }
                
                _logger.LogWarning("Invalid OTP attempted for {Email}", model.Email);
                ModelState.AddModelError("", "Mã OTP không chính xác hoặc đã hết hạn.");
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.ResetPasswordAsync(model.Email, model.OtpCode, model.NewPassword);
                
                if (result)
                {
                    _logger.LogInformation("Password reset successful for {Email}", model.Email);
                    TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
                    return RedirectToAction("Login");
                }
                
                _logger.LogWarning("Password reset failed for {Email}", model.Email);
                ModelState.AddModelError("", "Không thể đặt lại mật khẩu. Mã OTP đã hết hạn hoặc không hợp lệ.");
            }
            
            return View(model);
        }

        // Google Login
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };
            
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                // Get the authentication result that contains the user's information
                var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!authenticateResult.Succeeded)
                    return RedirectToAction("Login");

                var emailClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Email);
                var nameClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Name);
                var idClaim = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier);
                
                if (emailClaim == null || idClaim == null)
                {
                    _logger.LogWarning("Google authentication failed: No email or id claim");
                    // Loại bỏ TempData thông báo lỗi không cần thiết
                    return RedirectToAction("Login");
                }

                var email = emailClaim.Value;
                var name = nameClaim?.Value ?? "Google User";
                var providerKey = idClaim.Value;
                var loginProvider = GoogleDefaults.AuthenticationScheme;

                // Kiểm tra tài khoản đã tồn tại hay chưa
                var (user, isNewUser) = await _accountService.ExternalLoginCallbackAsync(email, name, loginProvider, providerKey);

                if (isNewUser)
                {
                    // Người dùng mới - chuyển đến trang chọn vai trò
                    var chooseRoleModel = new ChooseRoleExternalViewModel
                    {
                        Email = email,
                        FullName = name,
                        LoginProvider = loginProvider,
                        ProviderKey = providerKey
                    };
                    TempData["ChooseRoleExternalModel"] = JsonSerializer.Serialize(chooseRoleModel);
                    return RedirectToAction("ChooseRoleExternal");
                }
                else if (user != null)
                {
                    // Người dùng đã tồn tại - đăng nhập trực tiếp
                    await _accountService.SignInAsync(user);
                    
                    _logger.LogInformation("User {Email} logged in with Google", email);
                    // Loại bỏ TempData thông báo thành công không cần thiết
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Lỗi không xác định
                    _logger.LogWarning("Google login failed for {Email}: User not found and not marked as new", email);
                    // Loại bỏ TempData thông báo lỗi không cần thiết
                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication callback");
                // Loại bỏ TempData thông báo lỗi không cần thiết
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult ChooseRoleExternal()
        {
            if (TempData["ChooseRoleExternalModel"] is string modelJson)
            {
                var model = JsonSerializer.Deserialize<ChooseRoleExternalViewModel>(modelJson);
                if (model != null) 
                {
                    // Re-serialize to keep it in TempData in case of validation failure on POST
                    TempData["ChooseRoleExternalModel"] = modelJson; 
                    return View(model);
                }
            }
            // Chuyển hướng không hiển thị thông báo lỗi
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseRoleExternal(ChooseRoleExternalViewModel model)
        {
            // Keep the model in TempData in case of validation errors, so GET can re-display it
            // Only re-serialize if it's not already there from the GET request (e.g. if user tampered with hidden fields)
            if (TempData["ChooseRoleExternalModel"] == null && model != null)
            {
                 TempData["ChooseRoleExternalModel"] = JsonSerializer.Serialize(model);
            }

            if (ModelState.IsValid)
            {
                var result = await _accountService.CreateExternalUserAsync(model.Email, model.FullName, model.LoginProvider, model.ProviderKey, model.SelectedRole);
                if (result.Succeeded && result.User != null)
                {
                    await _accountService.SignInAsync(result.User);
                    TempData.Remove("ChooseRoleExternalModel"); // Clean up TempData
                    return RedirectToAction("Index", "Home");
                }
                if(result.Errors != null){
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Vui lòng thử lại.");
                }
            }
            // If model state is invalid, the model bound by ASP.NET Core will be passed back to the view.
            // Ensure TempData is kept so that if the user refreshes the GET page, it still has the data.
            // The hidden fields in the form will repopulate Email, FullName, LoginProvider, ProviderKey.
            return View(model);
        }
    }
}
