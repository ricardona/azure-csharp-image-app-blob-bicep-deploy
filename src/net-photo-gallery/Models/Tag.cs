using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic; // Keep for non-mapped ImageTags

namespace NETPhotoGallery.Models
{
    public class TagEntity : ITableEntity
    {
        // Name is unique, so it can be RowKey. PartitionKey can be a constant for all tags.
        public string Name { get; set; } = string.Empty;

        public string PartitionKey { get; set; } = "TAG"; // Constant PartitionKey for all tags
        public string RowKey { get; set; } = string.Empty; // Tag Name or a unique ID if Name is not unique enough
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Not directly mapped to Table Storage
        // public ICollection<ImageTagEntity> ImageTags { get; set; } = new List<ImageTagEntity>();
    }
}
