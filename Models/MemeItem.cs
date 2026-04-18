namespace MemeStash.Models;

public class MemeItem
{
    public required string BlobName { get; init; }
    public required string Slug { get; init; }
    public required string ContentType { get; init; }
    public string? OriginalFileName { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public long SizeBytes { get; init; }
    public bool IsPinned { get; init; }

    public string Url => $"/api/memes/{Slug}/{BlobName}";
}
