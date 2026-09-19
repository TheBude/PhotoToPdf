namespace PhotoToPdf.Api.Models;

public class UserHistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string OutputFileNames { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
