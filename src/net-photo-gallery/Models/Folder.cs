using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic; // Keep for non-mapped collections if needed for view models

namespace NETPhotoGallery.Models
{
    public class FolderEntity : ITableEntity
    {
        public string Name { get; set; } = string.Empty;

        // PartitionKey could be the ParentFolderId or a constant for root folders
        // RowKey could be the FolderId (e.g., a GUID or unique name)
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty; // Represents FolderId
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Not directly mapped to Table Storage, but can be populated by the service
        // public ICollection<FolderEntity> SubFolders { get; set; } = new List<FolderEntity>();
        // public string? ParentFolderName { get; set; } // If storing parent name for easier lookup
    }
}
