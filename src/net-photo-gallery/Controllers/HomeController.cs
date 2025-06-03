using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NETPhotoGallery.Models;
using NETPhotoGallery.Services;
using System.Diagnostics;
using Azure.Data.Tables; // Required for FolderEntity

namespace NETPhotoGallery.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureBlobService _azureBlobService;
        private readonly IImageLikeService _imageLikeService;
        private readonly IFolderService _folderService;
        private readonly ITagService _tagService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureBlobService azureBlobService, IImageLikeService imageLikeService, IFolderService folderService, ITagService tagService, ILogger<HomeController> logger)
        {
            _azureBlobService = azureBlobService;
            _imageLikeService = imageLikeService;
            _folderService = folderService;
            _tagService = tagService;
            _logger = logger;
        }

        public async Task<ActionResult> Index(string? currentFolderId = null, string? currentFolderName = null)
        {
            try
            {
                IEnumerable<FolderEntity> folders;
                if (string.IsNullOrEmpty(currentFolderId))
                {
                    folders = await _folderService.GetRootFoldersAsync();
                }
                else
                {
                    folders = await _folderService.GetSubFoldersAsync(currentFolderId);
                }

                var imageFolderPrefix = currentFolderName; // List images from the current folder
                var allBlobsTask = _azureBlobService.ListAsync(imageFolderPrefix);
                var allLikesTask = _imageLikeService.GetAllLikesAsync(); // Assuming likes are global for now
                
                await Task.WhenAll(allBlobsTask, allLikesTask);
                
                var allBlobs = await allBlobsTask;
                var likesMap = await allLikesTask;

                var blobViewModels = new List<BlobViewModel>();
                foreach (var blobUri in allBlobs) // Changed variable name for clarity
                {
                    // The imageId for tags and likes should ideally be the unique random file name,
                    // not including the folder path, as that's what AzureBlobService.UploadAsync now returns.
                    // Uri.Segments.Last() should give the file name part of the URI.
                    var imageFileName = Uri.UnescapeDataString(blobUri.Segments.Last());

                    var tags = await _tagService.GetTagsForImageAsync(imageFileName);
                    var likes = likesMap.GetValueOrDefault(imageFileName, 0);

                    blobViewModels.Add(new BlobViewModel
                    {
                        Uri = blobUri,
                        ImageId = imageFileName, // Use the extracted file name
                        Likes = likes,
                        Tags = tags ?? new List<TagEntity>() // Ensure Tags is not null
                    });
                }

                var viewModel = new GalleryViewModel
                {
                    Images = blobViewModels,
                    Folders = folders,
                    CurrentFolderId = currentFolderId,
                    CurrentFolderName = currentFolderName
                };
                
                // Parent folder logic can be added here if needed for "Up" navigation
                // For now, view will link to root if currentFolderId is present.

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index method");
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                Response.StatusCode = 500;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder(string folderName, string? parentFolderId, string? parentFolderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                // Consider adding a TempData error message here
                _logger.LogWarning("CreateFolder called with empty folderName.");
                return RedirectToAction("Index", new { currentFolderId = parentFolderId, currentFolderName = parentFolderName });
            }

            try
            {
                await _folderService.CreateFolderAsync(folderName, parentFolderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder.");
                // Consider adding a TempData error message here
            }
            return RedirectToAction("Index", new { currentFolderId = parentFolderId, currentFolderName = parentFolderName });
        }

        [HttpPost]
        [Route("Home/UploadAsync")] // Keeping existing route for now
        public async Task<ActionResult> UploadAsync(string? currentFolderId = null, string? currentFolderName = null, string? imageTagsString = null)
        {
            try
            {
                // Read imageTagsString from Form if not passed as parameter (e.g. if JS disabled or not setting it in query)
                if (string.IsNullOrEmpty(imageTagsString) && Request.Form.ContainsKey("imageTagsString"))
                {
                    imageTagsString = Request.Form["imageTagsString"].ToString();
                }

                // Try to get folder context from Form if not in parameters
                if (string.IsNullOrEmpty(currentFolderId) && Request.Form.ContainsKey("currentFolderId"))
                {
                    currentFolderId = Request.Form["currentFolderId"].ToString();
                }
                if (string.IsNullOrEmpty(currentFolderName) && Request.Form.ContainsKey("currentFolderName"))
                {
                    currentFolderName = Request.Form["currentFolderName"].ToString();
                }

                var request = await HttpContext.Request.ReadFormAsync();
                if (request.Files == null)
                {
                     _logger.LogWarning("UploadAsync called with no files in request.");
                    return BadRequest("Could not upload files");
                }

                var files = request.Files;
                if (files.Count == 0)
                {
                    _logger.LogWarning("UploadAsync called with empty files collection.");
                    return BadRequest("Could not upload empty files");
                }

                var uploadedBlobNames = await _azureBlobService.UploadAsync(files, currentFolderName); // Pass currentFolderName as prefix

                if (!string.IsNullOrWhiteSpace(imageTagsString) && uploadedBlobNames.Any())
                {
                    var tagNames = imageTagsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var blobName in uploadedBlobNames) // These are randomFileNames without prefix
                    {
                        foreach (var tagName in tagNames)
                        {
                            await _tagService.AddTagToImageAsync(blobName, tagName);
                        }
                    }
                }

                return RedirectToAction(nameof(Index), new { currentFolderId = currentFolderId, currentFolderName = currentFolderName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadAsync method");
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                Response.StatusCode = 500;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteImage(string fileUri)
        {
            try
            {
                await _azureBlobService.DeleteAsync(fileUri);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteImage method");
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                Response.StatusCode = 500; // set status code to 500
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await _azureBlobService.DeleteAllAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteAll method");
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                Response.StatusCode = 500; // set status code to 500
                return View("Error");
            }
        }        

        [HttpPost]
        public async Task<ActionResult> LikeImage(string imageId)
        {
            try
            {
                await _imageLikeService.AddLikeAsync(imageId);
                var likes = await _imageLikeService.GetLikesAsync(imageId);
                return Json(new { success = true, likes = likes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LikeImage method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> AddTagToImage(string imageId, string tagName)
        {
            if (string.IsNullOrWhiteSpace(imageId) || string.IsNullOrWhiteSpace(tagName))
            {
                return BadRequest("ImageId and TagName are required.");
            }

            try
            {
                await _tagService.AddTagToImageAsync(imageId, tagName);
                // CreateTagAsync should ideally just ensure the tag exists and return it.
                // If it creates, fine; if it exists, it should fetch and return.
                var tagEntity = await _tagService.CreateTagAsync(tagName);
                return Json(new { success = true, tag = new { name = tagEntity.Name, rowKey = tagEntity.RowKey } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AddTagToImage for ImageId: {imageId}, TagName: {tagName}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTagFromImage(string imageId, string tagName)
        {
            if (string.IsNullOrWhiteSpace(imageId) || string.IsNullOrWhiteSpace(tagName))
            {
                return BadRequest("ImageId and TagName are required.");
            }

            try
            {
                await _tagService.RemoveTagFromImageAsync(imageId, tagName);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in RemoveTagFromImage for ImageId: {imageId}, TagName: {tagName}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ImageDetails(string blobUri)
        {
            if (string.IsNullOrWhiteSpace(blobUri))
            {
                _logger.LogWarning("ImageDetails called with empty blobUri.");
                return NotFound();
            }

            Uri imageFullUri = new Uri(Uri.UnescapeDataString(blobUri));
            // ServiceImageId is the filename, used by LikeService and TagService.
            string imageFileName = System.IO.Path.GetFileName(imageFullUri.LocalPath);

            string? folderName = null;
            // Example URI: "http://account.blob.core.windows.net/container/folderName/fileName.jpg"
            // Segments: ["/", "container/", "folderName/", "fileName.jpg"] -> Length 4
            // If root image: "http://account.blob.core.windows.net/container/fileName.jpg"
            // Segments: ["/", "container/", "fileName.jpg"] -> Length 3
            if (imageFullUri.Segments.Length > 3)
            {
                // The segment before the filename is the potential folder name.
                string potentialFolderSegment = imageFullUri.Segments[imageFullUri.Segments.Length - 2];
                if (!string.IsNullOrEmpty(potentialFolderSegment))
                {
                    folderName = potentialFolderSegment.TrimEnd('/');
                    // Additional check: Ensure folderName is not the container name if images can be directly in container.
                    // This depends on AzureBlobConnectionFactory's container client.
                    // For now, this simple extraction is used. Consider if containerName can be `_azureBlobConnectionFactory.GetBlobContainer().Name`.
                    // If folderName == containerClient.Name, then it's not a subfolder.
                }
            }

            var likes = await _imageLikeService.GetLikesAsync(imageFileName); // Use imageFileName as ServiceImageId
            var tags = await _tagService.GetTagsForImageAsync(imageFileName); // Use imageFileName as ServiceImageId

            var viewModel = new ImageDetailsViewModel
            {
                ImageFileName = imageFileName, // Display name
                ServiceImageId = imageFileName, // ID for backend services
                ImageUri = imageFullUri,
                Likes = likes,
                Tags = tags ?? new List<TagEntity>(), // Ensure tags list is not null
                FolderName = folderName
            };

            return View(viewModel);
        }
    }
}
