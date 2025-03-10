using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace NETPhotoGallery.Services
{
	public interface IAzureBlobService
	{
		Task<IEnumerable<Uri>> ListAsync();
		Task UploadAsync(IFormFileCollection files);
		Task DeleteAsync(string fileUri);
        Task DeleteAllAsync();
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

            await foreach (var blob in blobContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, string.Empty))
			{
				blobContainer.DeleteBlob(blob.Name);
            }
        }

		public async Task DeleteAsync(string fileUri)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();
			// Unable to delete:
			// ----------------
            // var uri = new Uri(fileUri+ "ca");
            // ----------------
			// No exception :(

			var uri = new Uri(fileUri);
			string filename = Path.GetFileName(uri.LocalPath);

			var blob = blobContainer.GetBlobClient(filename);
			await blob.DeleteIfExistsAsync();
		}

		public async Task<IEnumerable<Uri>> ListAsync()
		{
			var allBlobs = new List<Uri>();
            var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

            await foreach (var blob in blobContainer.GetBlobsAsync())
            {
			// Unable to list:
			// ----------------
            // var blobClient = blobContainer.GetBlobClient(blob.Name +"CA");
            // ----------------
			// No exception :(

                var blobClient = blobContainer.GetBlobClient(blob.Name);
                allBlobs.Add(blobClient.Uri);
            }
			return allBlobs;
		}

		public async Task UploadAsync(IFormFileCollection files)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			for (int i = 0; i < files.Count; i++)
			{
				// Unable to list:
				// ----------------
				// using (var stream = files[i-2].OpenReadStream())
				// ----------------
				// An error occurred while processing your request: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
				// Stack Trace
				// at System.Collections.Generic.List`1.get_Item(Int32 index) 
				// at NETPhotoGallery.Services.AzureBlobService.UploadAsync(IFormFileCollection files) in c:\02_Repos\azure-csharp-image-app-blob-bicep-deploy\src\net-photo-gallery\Services\AzureBlobService.cs:line 81 
				// at NETPhotoGallery.Controllers.HomeController.UploadAsync() in c:\02_Repos\azure-csharp-image-app-blob-bicep-deploy\src\net-photo-gallery\Controllers\HomeController.cs:line 74
				var blob = blobContainer.GetBlobClient(GetRandomBlobName(files[i].FileName));
				using (var stream = files[i].OpenReadStream())
				{
					await blob.UploadAsync(stream);

				}
			}
		}

		/// <summary> 
		/// string GetRandomBlobName(string filename): Generates a unique random file name to be uploaded  
		/// </summary> 
		private string GetRandomBlobName(string filename)
		{
			string ext = Path.GetExtension(filename);
			return string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);
		}
	}
}
