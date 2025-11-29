using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FindjobnuService.Models
{
    public class Category
    {
        [Key]
        [Column("CategoryID")]
        public int CategoryID { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonIgnore]
        public ICollection<JobIndexPosts> JobIndexPosts { get; set; } = new List<JobIndexPosts>();
    }
}
