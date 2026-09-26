namespace InternFiesta.Models
{
    public class InternshipApplication
    {
        public int Id { get; set; }

        public int InternshipPostingId { get; set; }

        public string StudentUserId { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        public InternshipPosting? InternshipPosting { get; set; }
    }
}