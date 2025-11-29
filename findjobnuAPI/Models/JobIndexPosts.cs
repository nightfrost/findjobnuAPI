using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindjobnuService.Models
{
    public class JobIndexPosts
    {
        [Key]
        [Column("JobID")]
        public int JobID { get; set; }
        public string? CompanyName { get; set; } = string.Empty;
        public string? CompanyURL { get; set; } = string.Empty;
        public string? JobTitle { get; set; } = string.Empty;
        public string? JobDescription { get; set; } = string.Empty;
        public string? JobLocation { get; set; } = string.Empty;
        public string? JobUrl { get; set; } = string.Empty;
        public DateTime? Published { get; set; } = DateTime.UtcNow;
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public byte[]? BannerPicture { get; set; } = [];
        public byte[]? FooterPicture { get; set; } = [];
        public List<string>? Keywords { get; set; } = [];
    }
}
