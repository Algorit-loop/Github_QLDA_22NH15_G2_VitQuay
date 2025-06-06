using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppEL.Models;
using AppEL.Services;
using AppEL.ViewModels;

namespace AppEL.Controllers
{
    public class CourseController : Controller
    {
        private readonly JsonFileService _jsonFileService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CourseController> _logger;
        private readonly EnrollmentService _enrollmentService;
        private readonly AccountService _accountService;

        public CourseController(
            JsonFileService jsonFileService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CourseController> logger,
            EnrollmentService enrollmentService,
            AccountService accountService)
        {
            _jsonFileService = jsonFileService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _enrollmentService = enrollmentService;
            _accountService = accountService;
        }

        public IActionResult Index(CourseListViewModel model)
        {
            var courses = _jsonFileService.GetCourses().Where(c => c.IsPublished).ToList();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                courses = courses.Where(c => 
                    c.Title.Contains(model.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(model.SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(model.Category))
            {
                courses = courses.Where(c => c.Category == model.Category).ToList();
            }

            if (!string.IsNullOrWhiteSpace(model.Level))
            {
                courses = courses.Where(c => c.Level == model.Level).ToList();
            }

            // Apply sorting
            courses = (model.SortBy?.ToLower()) switch
            {
                "price" => courses.OrderBy(c => c.Price).ToList(),
                "price_desc" => courses.OrderByDescending(c => c.Price).ToList(),
                "rating" => courses.OrderByDescending(c => c.Rating).ToList(),
                "newest" => courses.OrderByDescending(c => c.CreatedAt).ToList(),
                "popular" => courses.OrderByDescending(c => c.EnrollmentCount).ToList(),
                _ => courses.OrderByDescending(c => c.CreatedAt).ToList(),
            };

            model.Courses = courses;
            return View(model);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Create()
        {
            return View(new CourseViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(CourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var course = new Course
                    {
                        Title = model.Title,
                        ShortDescription = model.ShortDescription,
                        Description = model.Description,
                        Price = model.Price,
                        Level = model.Level,
                        Category = model.Category,
                        Duration = model.Duration,
                        InstructorId = User.FindFirst("UserId")?.Value ?? "",
                        InstructorName = User.FindFirst("FullName")?.Value ?? "",
                        IsPublished = model.IsPublished
                    };

                    // Handle image upload
                    if (model.ImageFile != null)
                    {
                        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "courses");
                        Directory.CreateDirectory(uploadDir);

                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(stream);
                        }

                        course.ImageUrl = $"/uploads/courses/{fileName}";
                    }

                    var courses = _jsonFileService.GetCourses();
                    courses.Add(course);
                    _jsonFileService.SaveCourses(courses);

                    _logger.LogInformation("Course created successfully: {Title}", model.Title);
                    TempData["SuccessMessage"] = "Khóa học đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating course");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo khóa học. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Edit(string id)
        {
            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            // Check if user is instructor and owns the course
            if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
            {
                return Forbid();
            }

            var viewModel = new CourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                ShortDescription = course.ShortDescription,
                Description = course.Description,
                Price = course.Price,
                Level = course.Level,
                Category = course.Category,
                Duration = course.Duration,
                ImageUrl = course.ImageUrl,
                IsPublished = course.IsPublished
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(string id, CourseViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var courses = _jsonFileService.GetCourses();
                    var course = courses.FirstOrDefault(c => c.Id == id);

                    if (course == null)
                    {
                        return NotFound();
                    }

                    // Check if user is instructor and owns the course
                    if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
                    {
                        return Forbid();
                    }

                    // Update course properties
                    course.Title = model.Title;
                    course.ShortDescription = model.ShortDescription;
                    course.Description = model.Description;
                    course.Price = model.Price;
                    course.Level = model.Level;
                    course.Category = model.Category;
                    course.Duration = model.Duration;
                    course.IsPublished = model.IsPublished;
                    course.UpdatedAt = DateTime.Now;

                    // Handle image upload
                    if (model.ImageFile != null)
                    {
                        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "courses");
                        Directory.CreateDirectory(uploadDir);

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(course.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, course.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(stream);
                        }

                        course.ImageUrl = $"/uploads/courses/{fileName}";
                    }

                    _jsonFileService.SaveCourses(courses);

                    _logger.LogInformation("Course updated successfully: {Title}", model.Title);
                    TempData["SuccessMessage"] = "Khóa học đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating course");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khóa học. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Delete(string id)
        {
            try
            {
                var courses = _jsonFileService.GetCourses();
                var course = courses.FirstOrDefault(c => c.Id == id);
                
                if (course == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khóa học" });
                }

                // Check if user is instructor and owns the course
                if (!User.IsInRole("Admin"))
                {
                    var userId = User.FindFirst("UserId")?.Value;
                    if (course.InstructorId != userId)
                    {
                        return Json(new { success = false, message = "Bạn không có quyền xóa khóa học này" });
                    }
                }

                // Delete course image if exists
                if (!string.IsNullOrEmpty(course.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, course.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Delete course videos if exist
                foreach (var lesson in course.Lessons)
                {
                    if (!string.IsNullOrEmpty(lesson.VideoPath))
                    {
                        var videoPath = Path.Combine(_webHostEnvironment.WebRootPath, lesson.VideoPath.TrimStart('/'));
                        if (System.IO.File.Exists(videoPath))
                        {
                            System.IO.File.Delete(videoPath);
                        }
                    }
                }

                courses.Remove(course);
                _jsonFileService.SaveCourses(courses);

                _logger.LogInformation("Course {CourseId} deleted by {UserId}", id, User.FindFirst("UserId")?.Value);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khóa học" });
            }
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                var userName = User.FindFirst("FullName")?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }
                var users = _jsonFileService.GetUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }
                var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khóa học." });
                }
                if (user.Balance < course.Price)
                {
                    return Json(new { success = false, message = $"Số dư của bạn không đủ để đăng ký khóa học này. Số dư hiện tại: {user.Balance:N0} VNĐ" });
                }
                // Trừ tiền
                user.Balance -= course.Price;
                _jsonFileService.SaveUsers(users);
                await _enrollmentService.EnrollUserInCourse(userId, userName, courseId);

                // Re-login để cập nhật claim balance
                await _accountService.LoginAsync(user.Username, user.Password);

                return Json(new { success = true, message = "Bạn đã đăng ký khóa học thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling in course {CourseId}", courseId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký khóa học." });
            }
        }

        [Authorize]
        public IActionResult MyCourses()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID not found");
                }

                var enrollments = _enrollmentService.GetUserEnrollments(userId);
                return View(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user courses");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách khóa học";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize]
        public IActionResult Details(string id)
        {
            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new CourseDetailsViewModel
            {
                Course = course,
                IsEnrolled = false,
                Progress = 0,
                CompletedLessons = new List<string>()
            };

            // If user is logged in, check enrollment status
            var userId = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var enrollment = _enrollmentService.GetEnrollment(userId, id);
                    viewModel.IsEnrolled = true;
                    viewModel.Progress = enrollment.Progress;
                    viewModel.CompletedLessons = enrollment.CompletedLessons;
                }
                catch 
                {
                    // User is not enrolled, use default values
                }
            }

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> MarkLessonComplete(string courseId, string lessonId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User ID not found" });
                }

                await _enrollmentService.MarkLessonComplete(userId, courseId, lessonId);
                var enrollment = _enrollmentService.GetEnrollment(userId, courseId);
                var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
                var totalLessonsCount = course?.Lessons.Count(l => l.IsPublished) ?? 0;

                return Json(new { 
                    success = true, 
                    message = "Bài học đã được đánh dấu hoàn thành.", 
                    progress = enrollment.Progress,
                    completedLessonsCount = enrollment.CompletedLessons.Count,
                    totalLessonsCount = totalLessonsCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking lesson as complete");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UnmarkLessonComplete(string courseId, string lessonId)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User ID not found" });
                }

                await _enrollmentService.UnmarkLessonComplete(userId, courseId, lessonId);
                var enrollment = _enrollmentService.GetEnrollment(userId, courseId);
                var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
                var totalLessonsCount = course?.Lessons.Count(l => l.IsPublished) ?? 0;
                
                return Json(new { 
                    success = true, 
                    message = "Đã bỏ đánh dấu hoàn thành bài học.", 
                    progress = enrollment.Progress,
                    completedLessonsCount = enrollment.CompletedLessons.Count,
                    totalLessonsCount = totalLessonsCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmarking lesson as complete");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult ManageCourses()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            var courses = _jsonFileService.GetCourses();
            
            if (!isAdmin)
            {
                // For instructors, only show their own courses
                courses = courses.Where(c => c.InstructorId == userId).ToList();
            }
            
            return View(courses);
        }
    }
}
