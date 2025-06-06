using AppEL.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AppEL.Services
{
    public class AccountService
    {
        private readonly JsonFileService _jsonFileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(JsonFileService jsonFileService, IHttpContextAccessor httpContextAccessor)
        {
            _jsonFileService = jsonFileService;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<bool> RegisterAsync(string username, string email, string password, string fullName)
        {
            var users = _jsonFileService.GetUsers();
            
            if (users.Any(u => u.Username == username || u.Email == email))
                return false;

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = password,
                FullName = fullName,
                Role = UserRole.Student
            };

            users.Add(newUser);
            _jsonFileService.SaveUsers(users);

            // Auto login after registration
            await LoginAsync(username, password);
            return true;
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
