namespace AppEL.Models
{
    public class Course
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
        public decimal Price { get; set; }
        public string InstructorId { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
        public string Category { get; set; } = string.Empty;
        public int Duration { get; set; } // in hours
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; }
        public int EnrollmentCount { get; set; }
        public double Rating { get; set; }
    }
}
