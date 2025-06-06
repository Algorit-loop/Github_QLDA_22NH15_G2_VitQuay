using AppEL.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace AppEL.Services
{
    public class AccountService
    {
        private readonly JsonFileService _jsonFileService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AccountService> _logger;
        private readonly IConfiguration _configuration;
        private Dictionary<string, (string otp, DateTime expiry)> _otpStore = new();

        public AccountService(
            JsonFileService jsonFileService, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountService> logger,
            IConfiguration configuration)
        {
            _jsonFileService = jsonFileService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var users = _jsonFileService.GetUsers();
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
                return false;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id),
                new Claim("Balance", user.Balance.ToString("N0"))
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await _httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return true;
        }

        public async Task<(User? User, bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(string username, string password, string email, string fullName, int role)
        {
            var users = _jsonFileService.GetUsers();
            
            // Kiểm tra username đã tồn tại chưa
            if (users.Any(u => u.Username == username))
            {
                return (null, false, new[] { "Tên đăng nhập đã tồn tại." });
            }
            
            // Kiểm tra email đã được sử dụng chưa
            if (users.Any(u => u.Email == email))
            {
                return (null, false, new[] { "Email đã được sử dụng." });
            }

            // Đảm bảo vai trò là hợp lệ (0=Admin, 1=Giáo viên, 2=Học sinh)
            // Trong quá trình đăng ký thông thường, chỉ cho phép role 1 hoặc 2
            if (role != (int)UserRole.Instructor && role != (int)UserRole.Student)
            {
                role = (int)UserRole.Student; // Mặc định là học sinh nếu không hợp lệ
            }

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                // Mã hóa mật khẩu (nếu dùng BCrypt)
                Password = password,
                Email = email,
                FullName = fullName,
                Role = (UserRole)role,
                CreatedAt = DateTime.Now,
                Balance = 0
            };

            users.Add(newUser);
            _jsonFileService.SaveUsers(users);

            // Auto login after registration
            await SignInAsync(newUser);
            return (newUser, true, Enumerable.Empty<string>());
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<bool> SendPasswordResetOtpAsync(string email)
        {
            var users = _jsonFileService.GetUsers();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return false;

            string otp = GenerateOtp();
            _otpStore[email] = (otp, DateTime.Now.AddMinutes(5));

            try
            {
                await SendEmailAsync(email, "Mã xác nhận đặt lại mật khẩu", 
                    $"<h2>Xin chào {user.FullName},</h2>" +
                    $"<p>Mã xác nhận để đặt lại mật khẩu của bạn là: <strong style='font-size:20px;'>{otp}</strong></p>" +
                    "<p>Mã này có hiệu lực trong 5 phút.</p>" +
                    "<p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>" +
                    "<p>Trân trọng,<br>Đội ngũ E-Learning</p>");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset OTP to {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResendOtpAsync(string email)
        {
            return await SendPasswordResetOtpAsync(email);
        }

        public bool VerifyOtp(string email, string otp)
        {
            if (_otpStore.TryGetValue(email, out var otpInfo))
            {
                if (otpInfo.expiry >= DateTime.Now && otpInfo.otp == otp)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            if (!VerifyOtp(email, otp))
                return false;

            var users = _jsonFileService.GetUsers();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return false;

            user.Password = newPassword;
            _jsonFileService.SaveUsers(users);

            // Remove OTP from store
            _otpStore.Remove(email);

            return true;
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try 
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var port = int.Parse(smtpSettings["Port"]);
                var username = smtpSettings["Username"];
                
                // Thử lấy mật khẩu từ User Secrets hoặc biến môi trường trước
                var password = _configuration["SmtpSettings:Password"];
                
                // Nếu không có, thử lấy từ appsettings.json
                if (string.IsNullOrEmpty(password))
                {
                    password = smtpSettings["Password"];
                }
                
                // Nếu không có từ cả hai nguồn, thử lấy từ biến môi trường
                if (string.IsNullOrEmpty(password))
                {
                    password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
                }
                
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"]);
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"];
                
                if (string.IsNullOrEmpty(username) || 
                    string.IsNullOrEmpty(password))
                {
                    _logger.LogError("Cấu hình SMTP không hợp lệ. Hãy thiết lập thông tin mật khẩu qua User Secrets hoặc biến môi trường");
                    throw new InvalidOperationException("Cấu hình SMTP không hợp lệ");
                }

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Timeout = 10000 // 10 giây timeout
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                _logger.LogInformation("Đang gửi email đến {Email}", email);
                await client.SendMailAsync(message);
                _logger.LogInformation("Email đã được gửi thành công đến {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email đến {Email}: {Message}", email, ex.Message);
                throw; // Ném lại ngoại lệ để xử lý ở tầng trên
            }
        }

        public async Task<(User? User, bool IsNewUser)> ExternalLoginCallbackAsync(string email, string fullName, string loginProvider, string providerKey)
        {
            var users = _jsonFileService.GetUsers();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                // User exists, no need for role selection, just return the user
                return (user, false);
            }
            else
            {
                // User doesn't exist yet, return null to indicate role selection is needed
                return (null, true);
            }
        }

        public async Task<(User? User, bool Succeeded, IEnumerable<string>? Errors)> CreateExternalUserAsync(string email, string fullName, string loginProvider, string providerKey, int selectedRole)
        {
            var users = _jsonFileService.GetUsers();
            
            // Double check that the user doesn't exist
            if (users.Any(u => u.Email == email))
            {
                // If user already exists (unlikely but as a safety check)
                var existingUser = users.First(u => u.Email == email);
                return (existingUser, true, null);
            }

            // Đảm bảo vai trò là hợp lệ (1=Giáo viên, 2=Học sinh)
            // Không cho phép chọn Admin (0) qua đăng ký Google
            if (selectedRole != (int)UserRole.Instructor && selectedRole != (int)UserRole.Student)
            {
                selectedRole = (int)UserRole.Student; // Mặc định là học sinh nếu không hợp lệ
            }

            // Tạo một username duy nhất dựa trên loginProvider và hash một phần của providerKey
            var username = $"{loginProvider}-{providerKey.Substring(0, Math.Min(8, providerKey.Length))}";
            
            // Đảm bảo username là duy nhất
            int count = 1;
            string originalUsername = username;
            while (users.Any(u => u.Username == username))
            {
                username = $"{originalUsername}{count++}";
            }

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                // Tạo một mật khẩu ngẫu nhiên an toàn vì người dùng sẽ đăng nhập bằng OAuth
                Password = GenerateSecurePassword(),
                Email = email,
                FullName = fullName,
                Role = (UserRole)selectedRole,
                CreatedAt = DateTime.Now,
                Balance = 0
            };

            users.Add(newUser);
            _jsonFileService.SaveUsers(users);
            return (newUser, true, Enumerable.Empty<string>());
        }

        public async Task SignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id),
                new Claim("Balance", user.Balance.ToString("N0"))
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await _httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            var users = _jsonFileService.GetUsers();
            return users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        private string GenerateSecurePassword()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[24];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
