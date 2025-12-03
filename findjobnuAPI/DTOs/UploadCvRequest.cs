using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FindjobnuService.DTOs
{
    public class UploadCvRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }
    }
}
