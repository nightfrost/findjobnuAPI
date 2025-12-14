using Microsoft.AspNetCore.Mvc;

namespace FindjobnuService.DTOs.Requests
{
    /// <summary>
    /// Request model for CV file upload operations.
    /// </summary>
    public class UploadCvRequest
    {
        /// <summary>
        /// The CV file to be analyzed.
        /// </summary>
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }
}
