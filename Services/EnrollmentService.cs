using System.Text.Json;
using AppEL.Models;

namespace AppEL.Services
{
    public class EnrollmentService
    {
        private readonly JsonFileService _jsonFileService;
        private readonly string _enrollmentsPath;

        public EnrollmentService(JsonFileService jsonFileService, IWebHostEnvironment webHostEnvironment)
        {
            _jsonFileService = jsonFileService;
            _enrollmentsPath = Path.Combine(webHostEnvironment.ContentRootPath, "Data", "enrollments.json");
        }

        private List<Enrollment> GetEnrollments()
        {
            if (!System.IO.File.Exists(_enrollmentsPath))
            {
                return new List<Enrollment>();
            }
            var jsonString = System.IO.File.ReadAllText(_enrollmentsPath);
            return JsonSerializer.Deserialize<List<Enrollment>>(jsonString) ?? new List<Enrollment>();
        }

        private void SaveEnrollments(List<Enrollment> enrollments)
        {
            var jsonString = JsonSerializer.Serialize(enrollments, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_enrollmentsPath, jsonString);
        }

        public Task<Enrollment> EnrollUserInCourse(string userId, string userName, string courseId)
        {
            var enrollments = GetEnrollments();
            var courses = _jsonFileService.GetCourses();
            var course = courses.FirstOrDefault(c => c.Id == courseId) 
                ?? throw new Exception("Course not found");

            if (enrollments.Any(e => e.UserId == userId && e.CourseId == courseId))
            {
                throw new Exception("User is already enrolled in this course");
            }

            var enrollment = new Enrollment
            {
                UserId = userId,
                UserName = userName,
                CourseId = courseId,
                Course = course
            };

            enrollments.Add(enrollment);
            SaveEnrollments(enrollments);

            // Update course enrollment count
            course.EnrollmentCount++;
            _jsonFileService.SaveCourses(courses);

            return Task.FromResult(enrollment);
        }

        public Enrollment GetEnrollment(string userId, string courseId)
        {
            var enrollments = GetEnrollments();
            return enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId) 
                ?? throw new Exception("Enrollment not found");
        }

        public List<Enrollment> GetUserEnrollments(string userId)
        {
            var enrollments = GetEnrollments();
            var courses = _jsonFileService.GetCourses();
            
            return enrollments
                .Where(e => e.UserId == userId)
                .Select(e => {
                    e.Course = courses.FirstOrDefault(c => c.Id == e.CourseId) ?? new Course() { Id = e.CourseId };
                    return e;
                })
                .Where(e => e.Course != null)
                .ToList();
        }

        public Task MarkLessonComplete(string userId, string courseId, string lessonId)
        {
            var enrollments = GetEnrollments();
            var enrollment = enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId)
                ?? throw new Exception("Enrollment not found");

            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId)
                ?? throw new Exception("Course not found for progress calculation");

            if (!enrollment.CompletedLessons.Contains(lessonId))
            {
                enrollment.CompletedLessons.Add(lessonId);
                
                // Recalculate progress
                if (course.Lessons.Any())
                {
                    enrollment.Progress = (double)enrollment.CompletedLessons.Count / course.Lessons.Count * 100;
                }
                else
                {
                    enrollment.Progress = 0;
                }

                if (enrollment.CompletedLessons.Count == course.Lessons.Count)
                {
                    enrollment.CompletedAt = DateTime.Now;
                }
                
                SaveEnrollments(enrollments);
            }
            
            return Task.CompletedTask;
        }

        public Task UnmarkLessonComplete(string userId, string courseId, string lessonId)
        {
            var enrollments = GetEnrollments();
            var enrollment = enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId)
                ?? throw new Exception("Enrollment not found");

            var course = _jsonFileService.GetCourses().FirstOrDefault(c => c.Id == courseId)
                ?? throw new Exception("Course not found for progress calculation");

            if (enrollment.CompletedLessons.Contains(lessonId))
            {
                enrollment.CompletedLessons.Remove(lessonId);
                enrollment.CompletedAt = null; // Reset completion status

                // Recalculate progress
                if (course.Lessons.Any())
                {
                    enrollment.Progress = (double)enrollment.CompletedLessons.Count / course.Lessons.Count * 100;
                }
                else
                {
                    enrollment.Progress = 0;
                }

                SaveEnrollments(enrollments);
            }
            
            return Task.CompletedTask;
        }
    }
}
