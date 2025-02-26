using Azure.Data.Tables;
using NETPhotoGallery.Models;

namespace NETPhotoGallery.Services
{
    public interface IImageLikeService
    {
        Task<int> GetLikesAsync(string imageId);
        Task AddLikeAsync(string imageId);
    }

    public class ImageLikeService : IImageLikeService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "imagelikes";

        public ImageLikeService(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("StorageConnectionString");
            var tableServiceClient = new TableServiceClient(connectionString);
            tableServiceClient.CreateTableIfNotExists(TableName);
            _tableClient = tableServiceClient.GetTableClient(TableName);
        }

        public async Task<int> GetLikesAsync(string imageId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ImageLike>("images", imageId);
                return response.Value.LikeCount;
            }
            catch (Azure.RequestFailedException)
            {
                return 0;
            }
        }

        public async Task AddLikeAsync(string imageId)
        {
            var like = new ImageLike
            {
                PartitionKey = "images",
                RowKey = imageId,
                LikeCount = 1
            };

            try
            {
                var existingLike = await _tableClient.GetEntityAsync<ImageLike>("images", imageId);
                like.LikeCount = existingLike.Value.LikeCount + 1;
            }
            catch (Azure.RequestFailedException)
            {
                // Entity doesn't exist, use default count of 1
            }

            await _tableClient.UpsertEntityAsync(like);
        }
    }
}