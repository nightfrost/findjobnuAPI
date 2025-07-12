namespace findjobnuAPI.Models;

public class JobIndexPostsSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public string? Category { get; set; }
    public DateTime? PostedAfter { get; set; }
    public DateTime? PostedBefore { get; set; }
    public int Page { get; set; } = 1;
}