namespace InternFiesta.Models
{
    public class SavedInternship
    {
        public int Id { get; set; }

        public int InternshipPostingId { get; set; }

        public string StudentUserId { get; set; } = string.Empty;

        public DateTime SavedAt { get; set; } = DateTime.Now;

        public InternshipPosting? InternshipPosting { get; set; }
    }
}