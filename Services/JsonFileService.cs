using System.Text.Json;
using AppEL.Models;

namespace AppEL.Services
{
    public class JsonFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _userDataPath;
        private readonly string _courseDataPath;
        private readonly string _categoryDataPath;
        private readonly string _lessonVideoPath;

        public JsonFileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _userDataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "users.json");
            _courseDataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "courses.json");
            _categoryDataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "categories.json");
            _lessonVideoPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "coursevideo");

            // Create data directories if they don't exist
            var dataDir = Path.Combine(_webHostEnvironment.ContentRootPath, "Data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            // Create video directory if it doesn't exist
            if (!Directory.Exists(_lessonVideoPath))
            {
                Directory.CreateDirectory(_lessonVideoPath);
            }

            // Create JSON files if they don't exist
            if (!File.Exists(_userDataPath))
            {
                File.WriteAllText(_userDataPath, "[]");
            }
            if (!File.Exists(_courseDataPath))
            {
                File.WriteAllText(_courseDataPath, "[]");
            }
            if (!File.Exists(_categoryDataPath))
            {
                File.WriteAllText(_categoryDataPath, "[]");
            }
        }

        public List<User> GetUsers()
        {
            var jsonString = File.ReadAllText(_userDataPath);
            return JsonSerializer.Deserialize<List<User>>(jsonString) ?? new List<User>();
        }

        public void SaveUsers(List<User> users)
        {
            var jsonString = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_userDataPath, jsonString);
        }

        public List<Course> GetCourses()
        {
            var jsonString = File.ReadAllText(_courseDataPath);
            return JsonSerializer.Deserialize<List<Course>>(jsonString) ?? new List<Course>();
        }

        public void SaveCourses(List<Course> courses)
        {
            var jsonString = JsonSerializer.Serialize(courses, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_courseDataPath, jsonString);
        }

        public List<Category> GetCategories()
        {
            if (!File.Exists(_categoryDataPath))
            {
                return new List<Category>();
            }
            var jsonString = File.ReadAllText(_categoryDataPath);
            return JsonSerializer.Deserialize<List<Category>>(jsonString) ?? new List<Category>();
        }

        public void SaveCategories(List<Category> categories)
        {
            var jsonString = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_categoryDataPath, jsonString);
        }

        public bool DepositToUser(string userId, decimal amount)
        {
            var users = GetUsers();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;
            user.Balance += amount;
            SaveUsers(users);
            return true;
        }
    }
}
