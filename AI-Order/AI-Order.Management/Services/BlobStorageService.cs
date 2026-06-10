using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace AI_Order.Management.Services;

public class BlobStorageService
{
    private readonly string? _connectionString;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration config)
    {
        _connectionString = config["BlobStorage:ConnectionString"];
        _containerName = config["BlobStorage:ContainerName"] ?? "qrorder";
    }

    public async Task<string> UploadImageAsync(
        Stream stream, string contentType, string userId, string itemName, string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");

        var container = new BlobServiceClient(_connectionString)
            .GetBlobContainerClient(_containerName);

        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var slug = Slugify(itemName);
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var blobName = $"images/{userId}/{slug}/{Guid.NewGuid()}{ext}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        return blob.Uri.ToString();
    }

    public async Task DeleteImageAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(_connectionString) || string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        // Blob name is everything after /{containerName}/
        var prefix = $"/{_containerName}/";
        var idx = uri.AbsolutePath.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return;

        var blobName = uri.AbsolutePath[(idx + prefix.Length)..];
        var container = new BlobServiceClient(_connectionString)
            .GetBlobContainerClient(_containerName);

        await container.GetBlobClient(blobName).DeleteIfExistsAsync();
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "untitled";
        return Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
    }
}
