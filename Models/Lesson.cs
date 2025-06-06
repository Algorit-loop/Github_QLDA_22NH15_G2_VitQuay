using System.ComponentModel.DataAnnotations;

namespace AppEL.Models
{
    public class Lesson
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
