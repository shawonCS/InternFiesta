namespace InternFiesta.Models
{
    public class InterviewSchedule
    {
        public int Id { get; set; }

        public int InternshipApplicationId { get; set; }

        public DateTime InterviewDateTime { get; set; }

        public string LocationOrLink { get; set; }
            = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public InternshipApplication?
            InternshipApplication
        { get; set; }
    }
}