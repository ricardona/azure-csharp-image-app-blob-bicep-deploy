using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using NETPhotoGallery.Models;
using System; // For Guid
using System.Collections.Generic;
using System.Linq; // For Linq operations like .Select()
using System.Threading.Tasks;

namespace NETPhotoGallery.Services
{
    public interface IFolderService
    {
        Task<FolderEntity?> GetFolderAsync(string partitionKey, string rowKey);
        Task<List<FolderEntity>> GetRootFoldersAsync();
        Task<List<FolderEntity>> GetSubFoldersAsync(string parentFolderRowKey); // parentFolderRowKey is the RowKey of the parent
        Task<FolderEntity> CreateFolderAsync(string name, string? parentFolderRowKey);
        Task UpdateFolderAsync(FolderEntity folder);
        Task DeleteFolderAsync(string partitionKey, string rowKey);
        Task EnsureRootPartitionKeyAsync(); // Helper for initialization
    }

    public class FolderService : IFolderService
    {
        private readonly TableClient _folderTable;
        public const string ROOT_PARTITION_KEY = "ROOT_FOLDERS"; // PartitionKey for root folders

        public FolderService(ITableStorageService tableStorageService, IConfiguration configuration)
        {
            var folderTableName = configuration.GetValue<string>("StorageAccount:FolderTableName") ?? "Folders";
            _folderTable = tableStorageService.GetTableClient(folderTableName);
        }

        public async Task EnsureRootPartitionKeyAsync()
        {
            // This is a conceptual placeholder. PartitionKeys don't need explicit creation.
            // Entities are simply added with this PartitionKey.
            // We can ensure the table itself exists, which is done by TableStorageService.InitializeTableAsync
            await Task.CompletedTask;
        }

        public async Task<FolderEntity> CreateFolderAsync(string name, string? parentFolderRowKey)
        {
            var folder = new FolderEntity
            {
                RowKey = Guid.NewGuid().ToString(), // Unique ID for the folder
                Name = name,
                Timestamp = DateTimeOffset.UtcNow
            };

            if (string.IsNullOrEmpty(parentFolderRowKey)) // Root folder
            {
                folder.PartitionKey = ROOT_PARTITION_KEY;
            }
            else // Subfolder
            {
                // Parent's RowKey becomes the PartitionKey for its children,
                // allowing efficient querying of subfolders.
                folder.PartitionKey = parentFolderRowKey;
            }

            await _folderTable.AddEntityAsync(folder);
            return folder;
        }

        public async Task DeleteFolderAsync(string partitionKey, string rowKey)
        {
            // Consider recursive delete for subfolders if necessary.
            // This is complex with Table Storage and might require multiple queries.
            // For now, it only deletes the specified folder.
            // Also, consider what happens to images within this folder.
            await _folderTable.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<FolderEntity?> GetFolderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _folderTable.GetEntityAsync<FolderEntity>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<List<FolderEntity>> GetRootFoldersAsync()
        {
            return await _folderTable.QueryAsync<FolderEntity>(f => f.PartitionKey == ROOT_PARTITION_KEY).ToListAsync();
        }

        public async Task<List<FolderEntity>> GetSubFoldersAsync(string parentFolderRowKey)
        {
            // Subfolders have their Parent's RowKey as their PartitionKey
            return await _folderTable.QueryAsync<FolderEntity>(f => f.PartitionKey == parentFolderRowKey).ToListAsync();
        }

        public async Task UpdateFolderAsync(FolderEntity folder)
        {
            await _folderTable.UpdateEntityAsync(folder, folder.ETag, TableUpdateMode.Replace);
        }
    }
}
