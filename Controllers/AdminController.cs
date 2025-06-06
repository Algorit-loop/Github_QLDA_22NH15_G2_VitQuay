using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppEL.Models;
using AppEL.Services;
using AppEL.ViewModels;

namespace AppEL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly JsonFileService _jsonFileService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(JsonFileService jsonFileService, ILogger<AdminController> logger)
        {
            _jsonFileService = jsonFileService;
            _logger = logger;
        }

        // Quản lý người dùng
        public IActionResult Users()
        {
            var users = _jsonFileService.GetUsers();
            return View(users);
        }

        [HttpPost]
        public IActionResult ChangeUserRole(string userId, UserRole newRole)
        {
            try
            {
                var users = _jsonFileService.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user != null)
                {
                    user.Role = newRole;
                    _jsonFileService.SaveUsers(users);
                    _logger.LogInformation("Changed role for user {UserId} to {Role}", userId, newRole);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role");
                return Json(new { success = false, message = "Error changing user role" });
            }
        }

        [HttpPost]
        public IActionResult DeleteUser(string userId)
        {
            try
            {
                var users = _jsonFileService.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user != null)
                {
                    users.Remove(user);
                    _jsonFileService.SaveUsers(users);
                    _logger.LogInformation("Deleted user {UserId}", userId);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = "Error deleting user" });
            }
        }

        // Quản lý danh mục
        public IActionResult Categories()
        {
            var categories = _jsonFileService.GetCategories();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View(new Category());
        }

        [HttpPost]
        public IActionResult CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var categories = _jsonFileService.GetCategories();
                    
                    // Check if category ID already exists
                    if (categories.Any(c => c.Id == category.Id))
                    {
                        ModelState.AddModelError("Id", "Mã danh mục đã tồn tại");
                        return View(category);
                    }

                    categories.Add(category);
                    _jsonFileService.SaveCategories(categories);

                    _logger.LogInformation("Category created successfully: {Name}", category.Name);
                    TempData["SuccessMessage"] = "Danh mục đã được tạo thành công!";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo danh mục. Vui lòng thử lại.");
                }
            }

            return View(category);
        }

        public IActionResult EditCategory(string id)
        {
            var category = _jsonFileService.GetCategories().FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        public IActionResult EditCategory(string id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var categories = _jsonFileService.GetCategories();
                    var existingCategory = categories.FirstOrDefault(c => c.Id == id);

                    if (existingCategory == null)
                    {
                        return NotFound();
                    }

                    // Update category properties
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;

                    _jsonFileService.SaveCategories(categories);

                    _logger.LogInformation("Category updated successfully: {Name}", category.Name);
                    TempData["SuccessMessage"] = "Danh mục đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật danh mục. Vui lòng thử lại.");
                }
            }

            return View(category);
        }

        [HttpPost]
        public IActionResult DeleteCategory(string id)
        {
            try
            {
                var categories = _jsonFileService.GetCategories();
                var category = categories.FirstOrDefault(c => c.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Check if category is being used by any courses
                var courses = _jsonFileService.GetCourses();
                if (courses.Any(c => c.Category == category.Id))
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì đang được sử dụng bởi một số khóa học" });
                }

                categories.Remove(category);
                _jsonFileService.SaveCategories(categories);

                _logger.LogInformation("Category {CategoryId} deleted", id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa danh mục" });
            }
        }

        // Dashboard
        public IActionResult Index()
        {
            var users = _jsonFileService.GetUsers();
            var courses = _jsonFileService.GetCourses();
            var categories = _jsonFileService.GetCategories();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalCourses = courses.Count;
            ViewBag.TotalCategories = categories.Count;
            ViewBag.TotalInstructors = users.Count(u => u.Role == UserRole.Instructor);

            return View();
        }
    }
}
