using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using NETPhotoGallery.Models;
using System; // For Guid
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NETPhotoGallery.Services
{
    public interface ITagService
    {
        Task<TagEntity?> GetTagAsync(string tagRowKey); // Assuming RowKey is unique (e.g. normalized name or GUID)
        Task<List<TagEntity>> GetAllTagsAsync();
        Task<TagEntity> CreateTagAsync(string name);
        Task DeleteTagAsync(string tagRowKey);
        Task AddTagToImageAsync(string imageId, string tagName); // tagName will be used to find/create TagEntity's RowKey
        Task RemoveTagFromImageAsync(string imageId, string tagName);
        Task<List<TagEntity>> GetTagsForImageAsync(string imageId);
    }

    public class TagService : ITagService
    {
        private readonly TableClient _tagTable;
        private readonly TableClient _imageTagTable;
        public const string TAG_PARTITION_KEY = "TAGS"; // Constant PartitionKey for TagEntity

        public TagService(ITableStorageService tableStorageService, IConfiguration configuration)
        {
            var tagTableName = configuration.GetValue<string>("StorageAccount:TagTableName") ?? "Tags";
            var imageTagTableName = configuration.GetValue<string>("StorageAccount:ImageTagTableName") ?? "ImageTags";

            _tagTable = tableStorageService.GetTableClient(tagTableName);
            _imageTagTable = tableStorageService.GetTableClient(imageTagTableName);
        }

        public async Task<TagEntity> CreateTagAsync(string name)
        {
            // Normalize name to use as RowKey or generate a new GUID if names can have special chars not allowed in RowKey
            string rowKey = name.Replace(" ", "").ToUpperInvariant(); // Simple normalization
            var existingTag = await GetTagAsync(rowKey);
            if (existingTag != null) return existingTag;

            var tag = new TagEntity
            {
                PartitionKey = TAG_PARTITION_KEY,
                RowKey = rowKey, // Use normalized name or Guid.NewGuid().ToString()
                Name = name,
                Timestamp = DateTimeOffset.UtcNow
            };
            await _tagTable.AddEntityAsync(tag);
            return tag;
        }

        public async Task<TagEntity?> GetTagAsync(string tagRowKey)
        {
             try
            {
                var response = await _tagTable.GetEntityAsync<TagEntity>(TAG_PARTITION_KEY, tagRowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<List<TagEntity>> GetAllTagsAsync()
        {
            return await _tagTable.QueryAsync<TagEntity>(t => t.PartitionKey == TAG_PARTITION_KEY).ToListAsync();
        }

        public async Task DeleteTagAsync(string tagRowKey)
        {
            // First, remove all associations in ImageTags table
            var imageTagsQuery = _imageTagTable.QueryAsync<ImageTagEntity>(it => it.RowKey == tagRowKey); // Assuming RowKey in ImageTag is TagRowKey
            await foreach(var imageTag in imageTagsQuery)
            {
                await _imageTagTable.DeleteEntityAsync(imageTag.PartitionKey, imageTag.RowKey);
            }
            await _tagTable.DeleteEntityAsync(TAG_PARTITION_KEY, tagRowKey);
        }

        public async Task AddTagToImageAsync(string imageId, string tagName)
        {
            var tag = await CreateTagAsync(tagName); // Ensures tag exists, uses normalized RowKey

            var imageTag = new ImageTagEntity
            {
                PartitionKey = imageId, // ImageId
                RowKey = tag.RowKey,    // Tag's RowKey (normalized name or ID)
                Timestamp = DateTimeOffset.UtcNow
            };
            await _imageTagTable.UpsertEntityAsync(imageTag, TableUpdateMode.Replace);
        }

        public async Task RemoveTagFromImageAsync(string imageId, string tagName)
        {
            // Normalize tagName to find the tag's RowKey as it was stored
            string tagRowKey = tagName.Replace(" ", "").ToUpperInvariant();
            await _imageTagTable.DeleteEntityAsync(imageId, tagRowKey);
        }

        public async Task<List<TagEntity>> GetTagsForImageAsync(string imageId)
        {
            var imageTags = await _imageTagTable.QueryAsync<ImageTagEntity>(it => it.PartitionKey == imageId).ToListAsync();
            var tags = new List<TagEntity>();
            foreach (var imageTag in imageTags)
            {
                var tag = await GetTagAsync(imageTag.RowKey); // imageTag.RowKey is the Tag's RowKey
                if (tag != null)
                {
                    tags.Add(tag);
                }
            }
            return tags;
        }
    }
}
