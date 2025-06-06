using System.ComponentModel.DataAnnotations;
using AppEL.Models;

namespace AppEL.ViewModels
{
    public class CourseDetailsViewModel
    {
        public Course Course { get; set; } = null!;
        public bool IsEnrolled { get; set; }
        public double Progress { get; set; } // Changed from float to double
        public List<string> CompletedLessons { get; set; } = new();
    }

    public class CourseViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề khóa học")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn")]
        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string ShortDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá khóa học")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không được âm")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn cấp độ")]
        public string Level { get; set; } = "Beginner";

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thời lượng khóa học")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0")]
        public int Duration { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsPublished { get; set; }

        public IFormFile? ImageFile { get; set; }
    }

    public class CourseListViewModel
    {
        public List<Models.Course> Courses { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? Level { get; set; }
        public string? SortBy { get; set; }
        public bool IsManagement { get; set; }
    }

    public class CourseLessonsViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public List<Lesson> Lessons { get; set; } = new();
    }
}
