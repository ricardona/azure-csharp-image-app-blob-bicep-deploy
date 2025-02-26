using Azure;
using Azure.Data.Tables;

namespace NETPhotoGallery.Models
{
    public class ImageLike : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int LikeCount { get; set; }
    }
}