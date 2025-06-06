using System.ComponentModel.DataAnnotations;

namespace AppEL.ViewModels
{
    public class LessonViewModel
    {
        public string? Id { get; set; }
        public string CourseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài học")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả bài học")]
        public string Description { get; set; } = string.Empty;

        public string? VideoPath { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file video")]
        public IFormFile? VideoFile { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thứ tự bài học")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự bài học phải lớn hơn 0")]
        public int Order { get; set; }

        public bool IsPublished { get; set; }
    }
}
