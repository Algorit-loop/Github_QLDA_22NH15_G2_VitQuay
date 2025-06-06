using System;

namespace AppEL.Models
{
    public class Enrollment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public List<string> CompletedLessons { get; set; } = new();
        public float Progress => Course?.Lessons?.Count > 0 
            ? (float)CompletedLessons.Count / Course.Lessons.Count * 100 
            : 0;
        public Course Course { get; set; }
    }
}
