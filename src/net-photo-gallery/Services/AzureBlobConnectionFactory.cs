using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.ComponentModel;
using System.Xml.Linq;

namespace NETPhotoGallery.Services
{
	public interface IAzureBlobConnectionFactory
	{
		Task<BlobContainerClient> GetBlobContainer();
	}

	public class AzureBlobConnectionFactory : IAzureBlobConnectionFactory
	{
		private readonly IConfiguration _configuration;
		private BlobServiceClient _blobClient;
		private BlobContainerClient _blobContainer;

		public AzureBlobConnectionFactory(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task<BlobContainerClient> GetBlobContainer()
		{
			if (_blobContainer != null)
				return _blobContainer;

			var containerName = _configuration.GetValue<string>("ContainerName");
			if (string.IsNullOrWhiteSpace(containerName))
			{
				throw new ArgumentException("Configuration must contain ContainerName");
			}            

            _blobContainer = GetClient().GetBlobContainerClient(containerName);

            await _blobContainer.CreateIfNotExistsAsync();

            await _blobContainer.SetAccessPolicyAsync(PublicAccessType.BlobContainer);

            return _blobContainer;
		}

		private BlobServiceClient GetClient()
		{
			if (_blobClient != null)
				return _blobClient;

			var storageConnectionString = _configuration.GetValue<string>("StorageConnectionString");
			if(string.IsNullOrWhiteSpace(storageConnectionString))
			{
				throw new ArgumentException("Configuration must contain StorageConnectionString");
			}

			_blobClient = new BlobServiceClient(storageConnectionString); 

			return _blobClient;
		}
	}
}
