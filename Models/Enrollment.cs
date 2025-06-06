using System;

namespace AppEL.Models
{
    public class Enrollment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public required string CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public List<string> CompletedLessons { get; set; } = new();
        public double Progress { get; set; } = 0; // Changed to a settable property, type double for precision
        public required Course Course { get; set; }
    }
}
