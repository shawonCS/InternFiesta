using System.ComponentModel.DataAnnotations;

namespace InternFiesta.Models
{
    public class AcademicProgressAttachment
    {
        public int Id { get; set; }

        public int ProgressUpdateId { get; set; }
        public AcademicProgressUpdate ProgressUpdate { get; set; } = null!;

        [Required, StringLength(180)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
