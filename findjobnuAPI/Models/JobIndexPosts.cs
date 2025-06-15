namespace findjobnuAPI.Models
{
    public class JobIndexPosts
    {
        public int JobID { get; set; }
        public string? CompanyName { get; set; } = string.Empty;
        public string? CompanyURL { get; set; } = string.Empty;
        public string? JobTitle { get; set; } = string.Empty;
        public string? JobDescription { get; set; } = string.Empty;
        public string? JobLocation { get; set; } = string.Empty;
        public string? JobUrl { get; set; } = string.Empty;
        public DateTime? Published { get; set; } = DateTime.UtcNow;
        public string? Category { get; set; } = string.Empty;
        public Byte[]? BannerPicture { get; set; } = [];
        public Byte[]? FooterPicture { get; set; } = [];
    }
}
