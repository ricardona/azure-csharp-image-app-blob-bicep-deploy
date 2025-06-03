using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http; // Required for IFormFileCollection
using System; // Required for Uri, Path, Guid, DateTime
using System.Collections.Generic; // Required for List, IEnumerable
using System.IO; // Required for Path
using System.Linq; // Required for LINQ methods like Skip
using System.Threading.Tasks; // Required for Task

namespace NETPhotoGallery.Services
{
    public interface IAzureBlobService
    {
        // Pass folderPrefix (which could be folder's RowKey)
        Task<IEnumerable<Uri>> ListAsync(string? folderPrefix = null);
        Task<IEnumerable<string>> UploadAsync(IFormFileCollection files, string? folderPrefix = null);
        // fileUri will now potentially include the folder prefix
        Task DeleteAsync(string fileUri);
        Task DeleteAllAsync(); // This might need adjustment if "folders" should be handled differently
    }

    public class AzureBlobService : IAzureBlobService
    {
        private readonly IAzureBlobConnectionFactory _azureBlobConnectionFactory;

        public AzureBlobService(IAzureBlobConnectionFactory azureBlobConnectionFactory)
        {
            _azureBlobConnectionFactory = azureBlobConnectionFactory;
        }

        public async Task DeleteAllAsync()
        {
            var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
            // This deletes all blobs, regardless of folder structure.
            var blobsToDelete = new List<string>();
            await foreach (var blob in blobContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, string.Empty))
            {
                blobsToDelete.Add(blob.Name);
            }
            // Perform deletions after enumeration to avoid issues with modifying collection during iteration (if underlying SDK had such issues)
            foreach(var blobName in blobsToDelete)
            {
                await blobContainer.DeleteBlobIfExistsAsync(blobName);
            }
        }

        public async Task DeleteAsync(string fileUri)
        {
            var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
            var uri = new Uri(fileUri);

            // Extract blob name from URI. Assumes URI is like "https://account.blob.core.windows.net/container/folder/file.jpg"
            // The container name is the first segment of the path part of the URI if BlobContainerClient is for the root of storage account.
            // Or, if BlobContainerClient is already for a specific container, then blobName is everything after container name in path.
            // AzureBlobConnectionFactory.GetBlobContainer() likely returns a client for a specific container.
            // So, uri.AbsolutePath would be /containerName/folderPath/fileName.ext
            // We need to get the path relative to the container.
            string containerName = blobContainer.Name;
            string absolutePath = uri.AbsolutePath; // e.g., /mycontainer/myfolder/myfile.jpg
            int containerPathIndex = absolutePath.IndexOf(containerName);
            string blobNameFromUri = absolutePath.Substring(containerPathIndex + containerName.Length).TrimStart('/');

            var blobClient = blobContainer.GetBlobClient(blobNameFromUri);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<IEnumerable<Uri>> ListAsync(string? folderPrefix = null)
        {
            var blobUris = new List<Uri>();
            var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

            // Ensure folderPrefix, if provided, ends with a delimiter if it's not empty.
            string effectivePrefix = folderPrefix;
            if (!string.IsNullOrEmpty(effectivePrefix) && !effectivePrefix.EndsWith("/"))
            {
                effectivePrefix += "/";
            }
            // If folderPrefix was initially null or empty, effectivePrefix will also be null or empty.
            // GetBlobsByHierarchyAsync treats null and string.Empty prefix differently for listing from root.
            // For listing root content, prefix should be null or string.Empty.
            // If folderPrefix is null, effectivePrefix is null. If folderPrefix is "", effectivePrefix is "".
            // This behavior is consistent with how GetBlobsByHierarchyAsync handles root listing.

            await foreach (var blobHierarchyItem in blobContainer.GetBlobsByHierarchyAsync(prefix: effectivePrefix, delimiter: "/"))
            {
                if (blobHierarchyItem.IsBlob)
                {
                    // Only add actual blobs (images) to the list.
                    // Prefixes (subfolders) are ignored here as FolderService handles folder structure.
                    var blobClient = blobContainer.GetBlobClient(blobHierarchyItem.Blob.Name);
                    blobUris.Add(blobClient.Uri);
                }
            }
            return blobUris;
        }

        public async Task<IEnumerable<string>> UploadAsync(IFormFileCollection files, string? folderPrefix = null)
        {
            var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
            List<string> uploadedBlobNames = new List<string>();

            for (int i = 0; i < files.Count; i++)
            {
                string originalFileName = files[i].FileName;
                string randomFileName = GetRandomBlobName(originalFileName);
                uploadedBlobNames.Add(randomFileName); // Add the random file name (without prefix)

                string blobName = randomFileName;
                if (!string.IsNullOrEmpty(folderPrefix))
                {
                    var cleanFolderPrefix = folderPrefix.Trim('/');
                    if(!string.IsNullOrWhiteSpace(cleanFolderPrefix))
                    {
                       blobName = $"{cleanFolderPrefix}/{randomFileName}";
                    }
                }

                var blobClient = blobContainer.GetBlobClient(blobName);
                using (var stream = files[i].OpenReadStream())
                {
                    // Pass true for overwrite: await blobClient.UploadAsync(stream, true);
                    // Or use BlobUploadOptions for more control
                    await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = files[i].ContentType } });
                }
            }
            return uploadedBlobNames;
        }

        private string GetRandomBlobName(string filename)
        {
            string ext = Path.GetExtension(filename);
            return string.Format("{0}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid().ToString("N"), ext); // N for no hyphens in GUID
        }
    }
}
