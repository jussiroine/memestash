namespace MemeStash.Models;

public record SlugSummary
{
    public required string Slug { get; init; }
    public int MemeCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTimeOffset LastUploadedAt { get; init; }

    public string TotalSizeFormatted => TotalSizeBytes switch
    {
        < 1024 => $"{TotalSizeBytes} B",
        < 1024 * 1024 => $"{TotalSizeBytes / 1024.0:F1} KB",
        _ => $"{TotalSizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
