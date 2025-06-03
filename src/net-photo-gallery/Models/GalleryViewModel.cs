namespace NETPhotoGallery.Models
{
    public class GalleryViewModel
    {
        public IEnumerable<BlobViewModel> Images { get; set; } = new List<BlobViewModel>();
        public IEnumerable<FolderEntity> Folders { get; set; } = new List<FolderEntity>();
        public string? CurrentFolderId { get; set; }
        public string? CurrentFolderName { get; set; }
        public string? ParentFolderId { get; set; } // For "Up" navigation
        public string? ParentFolderName { get; set; } // For "Up" navigation
    }
}
