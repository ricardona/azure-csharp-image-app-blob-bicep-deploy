using NETPhotoGallery.Models; // For TagEntity
using System;
using System.Collections.Generic;

namespace NETPhotoGallery.Models
{
    public class ImageDetailsViewModel
    {
        public string ImageFileName { get; set; } = string.Empty;
        public Uri ImageUri { get; set; } = default!;
        public int Likes { get; set; }
        public List<TagEntity> Tags { get; set; } = new List<TagEntity>();
        public string? FolderName { get; set; } // Optional: if image is in a folder

        // The ImageId used by LikeService and TagService (usually just the filename)
        public string ServiceImageId { get; set; } = string.Empty;
    }
}
