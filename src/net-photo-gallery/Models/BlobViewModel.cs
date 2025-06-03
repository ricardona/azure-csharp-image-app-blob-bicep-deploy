namespace NETPhotoGallery.Models
{
    public class BlobViewModel
    {
        public Uri Uri { get; set; } = default!;
        public int Likes { get; set; }
        public string ImageId { get; set; } = string.Empty;
        public List<TagEntity> Tags { get; set; } = new List<TagEntity>();
    }
}