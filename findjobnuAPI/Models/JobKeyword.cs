using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindjobnuService.Models
{
    [Table("JobKeywords")]
    public class JobKeyword
    {
        [Key]
        public int KeywordID { get; set; }
        public int JobID { get; set; }
        [Required]
        [MaxLength(255)]
        public string Keyword { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? Source { get; set; }
        public double? ConfidenceScore { get; set; }
    }
}
