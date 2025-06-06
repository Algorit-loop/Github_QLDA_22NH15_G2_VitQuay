using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppEL.Models;
using AppEL.Services;
using AppEL.ViewModels;

namespace AppEL.Controllers
{
    public class LessonController : Controller
    {
        private readonly JsonFileService _jsonFileService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<LessonController> _logger;

        public LessonController(
            JsonFileService jsonFileService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<LessonController> logger)
        {
            _jsonFileService = jsonFileService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Index(string courseId)
        {
            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                return NotFound();
            }

            // Check if user is instructor and owns the course
            if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
            {
                return Forbid();
            }

            var viewModel = new CourseLessonsViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Lessons = course.Lessons.OrderBy(l => l.Order).ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Create(string courseId)
        {
            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                return NotFound();
            }

            // Check if user is instructor and owns the course
            if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
            {
                return Forbid();
            }

            var viewModel = new LessonViewModel
            {
                CourseId = courseId,
                Order = course.Lessons.Count + 1
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(LessonViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var courses = _jsonFileService.GetCourses();
                    var course = courses.FirstOrDefault(c => c.Id == model.CourseId);

                    if (course == null)
                    {
                        return NotFound();
                    }

                    // Check if user is instructor and owns the course
                    if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
                    {
                        return Forbid();
                    }

                    var lesson = new Lesson
                    {
                        CourseId = model.CourseId,
                        Title = model.Title,
                        Description = model.Description,
                        Order = model.Order,
                        IsPublished = model.IsPublished
                    };

                    // Handle video upload
                    if (model.VideoFile != null)
                    {
                        var courseVideoDir = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "coursevideo", model.CourseId);
                        Directory.CreateDirectory(courseVideoDir);

                        var fileName = $"{lesson.Id}{Path.GetExtension(model.VideoFile.FileName)}";
                        var filePath = Path.Combine(courseVideoDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.VideoFile.CopyToAsync(stream);
                        }

                        lesson.VideoPath = $"/Data/coursevideo/{model.CourseId}/{fileName}";
                    }

                    course.Lessons.Add(lesson);
                    _jsonFileService.SaveCourses(courses);

                    _logger.LogInformation("Lesson created successfully: {Title}", model.Title);
                    TempData["SuccessMessage"] = "Bài học đã được tạo thành công!";
                    return RedirectToAction("Details", "Course", new { id = model.CourseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating lesson");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo bài học. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Edit(string courseId, string id)
        {
            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                return NotFound();
            }

            var lesson = course.Lessons.FirstOrDefault(l => l.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }

            // Check if user is instructor and owns the course
            if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
            {
                return Forbid();
            }

            var viewModel = new LessonViewModel
            {
                Id = lesson.Id,
                CourseId = courseId,
                Title = lesson.Title,
                Description = lesson.Description,
                Order = lesson.Order,
                IsPublished = lesson.IsPublished,
                VideoPath = lesson.VideoPath // Ensure VideoPath is populated
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Edit(string courseId, string id, LessonViewModel model)
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
                    var course = courses.FirstOrDefault(c => c.Id == courseId);

                    if (course == null)
                    {
                        return NotFound();
                    }

                    var lesson = course.Lessons.FirstOrDefault(l => l.Id == id);
                    if (lesson == null)
                    {
                        return NotFound();
                    }

                    // Check if user is instructor and owns the course
                    if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
                    {
                        return Forbid();
                    }

                    // Update lesson properties
                    lesson.Title = model.Title;
                    lesson.Description = model.Description;
                    lesson.Order = model.Order;
                    lesson.IsPublished = model.IsPublished;
                    lesson.UpdatedAt = DateTime.Now;

                    // Handle video upload
                    if (model.VideoFile != null)
                    {
                        var courseVideoDir = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "coursevideo", courseId);
                        Directory.CreateDirectory(courseVideoDir);

                        // Delete old video if exists
                        if (!string.IsNullOrEmpty(lesson.VideoPath))
                        {
                            var oldVideoPath = Path.Combine(_webHostEnvironment.ContentRootPath, lesson.VideoPath.TrimStart('/')); // Use ContentRootPath
                            if (System.IO.File.Exists(oldVideoPath))
                            {
                                System.IO.File.Delete(oldVideoPath);
                            }
                        }

                        var fileName = $"{lesson.Id}{Path.GetExtension(model.VideoFile.FileName)}";
                        var filePath = Path.Combine(courseVideoDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.VideoFile.CopyToAsync(stream);
                        }

                        lesson.VideoPath = $"/Data/coursevideo/{courseId}/{fileName}";
                    }

                    _jsonFileService.SaveCourses(courses);

                    _logger.LogInformation("Lesson updated successfully: {Title}", model.Title);
                    TempData["SuccessMessage"] = "Bài học đã được cập nhật thành công!";
                    return RedirectToAction("Details", "Course", new { id = courseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating lesson");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật bài học. Vui lòng thử lại.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public IActionResult Delete(string courseId, string id)
        {
            try
            {
                var courses = _jsonFileService.GetCourses();
                var course = courses.FirstOrDefault(c => c.Id == courseId);
                
                if (course == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khóa học" });
                }

                var lesson = course.Lessons.FirstOrDefault(l => l.Id == id);
                if (lesson == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài học" });
                }

                // Check if user is instructor and owns the course
                if (User.IsInRole("Instructor") && course.InstructorId != User.FindFirst("UserId")?.Value)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa bài học này" });
                }

                // Delete video if exists
                if (!string.IsNullOrEmpty(lesson.VideoPath))
                {
                    try
                    {
                        var videoPath = Path.Combine(_webHostEnvironment.ContentRootPath, lesson.VideoPath.TrimStart('/'));
                        if (System.IO.File.Exists(videoPath))
                        {
                            System.IO.File.Delete(videoPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete video file for lesson {LessonId}", id);
                        // Continue with lesson deletion even if video file deletion fails
                    }
                }

                course.Lessons.Remove(lesson);
                _jsonFileService.SaveCourses(courses);

                _logger.LogInformation("Lesson {LessonId} deleted from course {CourseId}", id, courseId);
                TempData["SuccessMessage"] = $"Bài học '{lesson.Title}' đã được xóa thành công!";
                return Json(new { success = true, message = $"Bài học '{lesson.Title}' đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson {LessonId} from course {CourseId}", id, courseId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa bài học" });
            }
        }
    }
}
