namespace findjobnuAPI.Models
{
    public struct CategoriesResponse(bool success, string? errorMessage, Dictionary<string, int> categoryAndAmountOfJobs)
    {
        public bool Success { get; set; } = success;
        public string? ErrorMessage { get; set; } = errorMessage;
        public Dictionary<string, int> CategoryAndAmountOfJobs { get; set; } = categoryAndAmountOfJobs;
    }
}
