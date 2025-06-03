using Azure;
using Azure.Data.Tables;
using System;

namespace NETPhotoGallery.Models
{
    public class ImageTagEntity : ITableEntity
    {
        // PartitionKey could be ImageId to quickly find all tags for an image
        // RowKey could be TagId (or TagName if unique and preferred)
        public string PartitionKey { get; set; } = string.Empty; // ImageId
        public string RowKey { get; set; } = string.Empty;       // TagId (or TagName)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Store actual names if needed for denormalization, or rely on joins in service layer
        // public string ImageName { get; set; }
        // public string TagName { get; set; }
    }
}
