namespace MemeStash.Models;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public required string ConnectionString { get; set; }
    public string ContainerName { get; set; } = "memes";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    public int MaxMemesPerSlug { get; set; } = 100;
    public long MaxStoragePerSlugBytes { get; set; } = 100 * 1024 * 1024; // 100 MB
}
