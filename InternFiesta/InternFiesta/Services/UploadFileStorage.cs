using System.IO.Compression;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace InternFiesta.Services
{
    // Local demo storage. For production, set FileStorage:Root to persistent,
    // access-controlled storage outside wwwroot and add malware scanning.
    public sealed class UploadFileStorage
    {
        public const long AvatarLimit = 5 * 1024 * 1024;
        public const long EvidenceLimit = 5 * 1024 * 1024;
        public const int MaxEvidenceFiles = 3;

        private readonly string _avatars;
        private readonly string _evidence;

        public UploadFileStorage(IWebHostEnvironment env, IConfiguration configuration)
        {
            string root = configuration["FileStorage:Root"] ??
                Path.Combine(env.ContentRootPath, "App_Data", "Uploads");

            root = Path.GetFullPath(root);
            string webRoot = Path.GetFullPath(env.WebRootPath ??
                Path.Combine(env.ContentRootPath, "wwwroot"));

            if (root.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
                root.StartsWith(webRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "FileStorage:Root cannot be inside wwwroot.");
            }

            _avatars = Path.Combine(root, "Avatars");
            _evidence = Path.Combine(root, "ProgressEvidence");
            Directory.CreateDirectory(_avatars);
            Directory.CreateDirectory(_evidence);
        }

        public async Task<string> SaveAvatarAsync(
            IFormFile photo, CancellationToken cancellationToken = default)
        {
            if (photo.Length == 0 || photo.Length > AvatarLimit)
                throw new InvalidDataException(
                    "Choose a photo no larger than 5 MB.");
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png"))
                throw new InvalidDataException("Profile pictures must be JPG or PNG.");

            byte[] bytes = await ReadBytesAsync(photo, cancellationToken);
            if (!MatchesImageSignature(extension, bytes))
                throw new InvalidDataException("The image content does not match its extension.");

            using var input = new MemoryStream(bytes);
            // Identify dimensions before decoding to reduce image-bomb risk.
            SixLabors.ImageSharp.Image image;
            try
            {
                var info = await SixLabors.ImageSharp.Image.IdentifyAsync(
                    input, cancellationToken);
                if (info == null || (long)info.Width * info.Height > 20_000_000)
                    throw new InvalidDataException("The photo dimensions are too large.");

                input.Position = 0;
                image = await SixLabors.ImageSharp.Image.LoadAsync(
                    input, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidDataException("The photo could not be decoded or is too large.", ex);
            }

            // Re-encoding removes metadata and produces a consistent avatar.
            using (image)
            {

                image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
                {
                    Size = new Size(256, 256),
                    Mode = ResizeMode.Crop
                }));
                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.XmpProfile = null;

                string name = Guid.NewGuid().ToString("N") + ".jpg";
                string path = Path.Combine(_avatars, name);
                try
                {
                    await image.SaveAsJpegAsync(path,
                        new JpegEncoder { Quality = 85 }, cancellationToken);
                    return name;
                }
                catch
                {
                    TryDelete(path);
                    throw;
                }
            }
        }

        public async Task<SavedEvidence> SaveEvidenceAsync(
            IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file.Length == 0 || file.Length > EvidenceLimit)
                throw new InvalidDataException(
                    "Each supporting document must be smaller than 5 MB.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".pdf" or ".docx" or ".jpg" or ".jpeg" or ".png"))
                throw new InvalidDataException(
                    "Allowed evidence formats: PDF, DOCX, JPG, PNG.");

            byte[] bytes = await ReadBytesAsync(file, cancellationToken);
            if (!MatchesEvidenceSignature(extension, bytes))
                throw new InvalidDataException(
                    "A file has an invalid format or does not match its extension.");

            string type = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".png" => "image/png",
                _ => "image/jpeg"
            };

            // Never use the client-supplied filename as a disk path.
            string originalName = Path.GetFileName(file.FileName.Replace('\\', '/'));
            originalName = new string(originalName
                .Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (originalName.Length > 180) originalName = originalName[..180];
            if (string.IsNullOrWhiteSpace(originalName))
                originalName = "attachment" + extension;

            string name = Guid.NewGuid().ToString("N") + extension;
            string path = Path.Combine(_evidence, name);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            return new SavedEvidence(name, originalName, type, bytes.Length);
        }

        private static async Task<byte[]> ReadBytesAsync(
            IFormFile file, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            return stream.ToArray();
        }

        private static bool MatchesImageSignature(string ext, byte[] b)
        {
            if (ext == ".png")
                return b.AsSpan().StartsWith(new byte[]
                    { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
            return b.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff });
        }

        private static bool MatchesEvidenceSignature(string ext, byte[] b)
        {
            if (ext is ".jpg" or ".jpeg" or ".png")
                return MatchesImageSignature(ext, b);

            if (ext == ".pdf")
                return b.AsSpan().StartsWith("%PDF-"u8);

            if (ext == ".docx")
            {
                if (!b.AsSpan().StartsWith(new byte[] { 0x50, 0x4b, 0x03, 0x04 }))
                    return false;

                try
                {
                    using var memory = new MemoryStream(b);
                    using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
                    // Reject unusually large decompression totals / entry counts.
                    if (archive.Entries.Count > 1000) return false;
                    long total = 0;
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Length > 40L * 1024 * 1024 - total)
                            return false;
                        total += entry.Length;
                    }

                    return archive.GetEntry("[Content_Types].xml") != null &&
                           archive.GetEntry("word/document.xml") != null;
                }
                catch (InvalidDataException)
                {
                    return false;
                }
            }
            return false;
        }

        public string? AvatarPath(string? filename) =>
            IsGeneratedName(filename, new[] { ".jpg" })
                ? Path.Combine(_avatars, filename!) : null;

        public string? EvidencePath(string? filename) =>
            IsGeneratedName(filename,
                new[] { ".pdf", ".docx", ".jpg", ".jpeg", ".png" })
                ? Path.Combine(_evidence, filename!) : null;

        private static bool IsGeneratedName(string? name, string[] extensions)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string extension = Path.GetExtension(name);
            return extensions.Contains(extension, StringComparer.Ordinal) &&
                Regex.IsMatch(Path.GetFileNameWithoutExtension(name), "^[a-f0-9]{32}$");
        }

        public void DeleteAvatar(string? filename)
        {
            var path = AvatarPath(filename);
            if (path != null) TryDelete(path);
        }

        public void DeleteEvidence(string? filename)
        {
            var path = EvidencePath(filename);
            if (path != null) TryDelete(path);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { /* Log/clean up orphaned files later. */ }
            catch (UnauthorizedAccessException) { /* Storage permissions issue. */ }
        }
    }

    public sealed record SavedEvidence(
        string StoredFileName,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes);
}
