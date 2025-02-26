using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NETPhotoGallery.Models;
using NETPhotoGallery.Services;
using System.Diagnostics;

namespace NETPhotoGallery.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureBlobService _azureBlobService;
        private readonly IImageLikeService _imageLikeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureBlobService azureBlobService, IImageLikeService imageLikeService, ILogger<HomeController> logger)
        {
            _azureBlobService = azureBlobService;
            _imageLikeService = imageLikeService;
            _logger = logger;
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                var allBlobs = await _azureBlobService.ListAsync();
                var blobsWithLikes = new List<BlobViewModel>();
                
                foreach (var blob in allBlobs)
                {
                    var imageId = blob.Segments.Last();
                    var likes = await _imageLikeService.GetLikesAsync(imageId);
                    blobsWithLikes.Add(new BlobViewModel { Uri = blob, Likes = likes });
                }
                
                return View(blobsWithLikes);
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
        [Route("Home/UploadAsync")]
        public async Task<ActionResult> UploadAsync()
        {
            try
            {
                var request = await HttpContext.Request.ReadFormAsync();
                if (request.Files == null)
                {
                    return BadRequest("Could not upload files");
                }

                var files = request.Files;
                if (files.Count == 0)
                {
                    return BadRequest("Could not upload empty files");
                }

                await _azureBlobService.UploadAsync(files);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadAsync method");
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                Response.StatusCode = 500; // set status code to 500
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
    }
}
