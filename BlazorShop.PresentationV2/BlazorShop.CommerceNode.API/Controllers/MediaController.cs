namespace BlazorShop.CommerceNode.API.Controllers
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.CommerceNode.API.Validation;

    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/commerce/admin/media")]
    public sealed class MediaController : CommerceAdminControllerBase
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
            ["image/bmp"] = ".bmp",
        };

        private readonly IWebHostEnvironment environment;

        public MediaController(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public sealed class ImageUploadForm
        {
            public IFormFile? File { get; set; }
        }

        [HttpPost("images")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadForm form)
        {
            var file = form?.File;
            if (file is null || file.Length == 0)
            {
                return this.Failure<FileUploadResponse>(ServiceResponseType.ValidationError, "No file uploaded.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return this.Failure<FileUploadResponse>(
                    ServiceResponseType.ValidationError,
                    $"File too large. Max {MaxFileSizeBytes / (1024 * 1024)}MB.");
            }

            if (!AllowedExtensionsByContentType.TryGetValue(file.ContentType, out var safeExtension))
            {
                return this.Failure<FileUploadResponse>(
                    ServiceResponseType.ValidationError,
                    "Invalid file type. Only image files are allowed.");
            }

            await using (var validationStream = file.OpenReadStream())
            {
                var isValidImage = await ImageFileSignatureValidator.IsValidAsync(
                    validationStream,
                    file.ContentType,
                    this.HttpContext.RequestAborted);

                if (!isValidImage)
                {
                    return this.Failure<FileUploadResponse>(
                        ServiceResponseType.ValidationError,
                        "Invalid image content. The uploaded file does not match its declared type.");
                }
            }

            var uploadsPath = Path.Combine(this.environment.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var uniqueName = $"{Guid.NewGuid():N}{safeExtension}";
            var filePath = Path.Combine(uploadsPath, uniqueName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream, this.HttpContext.RequestAborted);
            }

            string fileUrl;
#if DEBUG
            fileUrl = $"{this.Request.Scheme}://{this.Request.Host}/uploads/{uniqueName}";
#else
            fileUrl = $"/uploads/{uniqueName}";
#endif

            var response = new FileUploadResponse(true, "File uploaded successfully.", fileUrl);
            return this.Success(response, response.Message);
        }
    }
}
