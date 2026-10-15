using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ExpenseTracker.Api.Services;

// Wraps Blob Storage. Auth is via Managed Identity / developer login - no account key anywhere.
public class BlobFileStore(BlobServiceClient service, IConfiguration config)
{
    private readonly BlobContainerClient container =
        service.GetBlobContainerClient(config["Storage:Container"]);

    private UserDelegationKey? delegationKey;
    private readonly SemaphoreSlim keyLock = new(1, 1);

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        await container.GetBlobClient(blobName).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
    }

    public Task DeleteAsync(string blobName, CancellationToken ct = default) =>
        container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);

    // Short-lived read-only link the browser can use directly in <img>/<audio>
    public async Task<string> GetReadUrlAsync(string blobName, TimeSpan validFor, CancellationToken ct = default)
    {
        var key = await GetDelegationKeyAsync(ct);
        var sas = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),   // tolerate clock skew
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor),
        };
        sas.SetPermissions(BlobSasPermissions.Read);

        var uri = new BlobUriBuilder(container.GetBlobClient(blobName).Uri)
        {
            Sas = sas.ToSasQueryParameters(key, service.AccountName)
        };
        return uri.ToUri().ToString();
    }

    // A user delegation key is signed by Entra ID instead of the account key; cache it and refresh before expiry
    private async Task<UserDelegationKey> GetDelegationKeyAsync(CancellationToken ct)
    {
        if (delegationKey is not null && delegationKey.SignedExpiresOn > DateTimeOffset.UtcNow.AddMinutes(30))
            return delegationKey;

        await keyLock.WaitAsync(ct);
        try
        {
            if (delegationKey is null || delegationKey.SignedExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(30))
            {
                delegationKey = (await service.GetUserDelegationKeyAsync(
                    DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(6), ct)).Value;
            }
            return delegationKey;
        }
        finally
        {
            keyLock.Release();
        }
    }
}
