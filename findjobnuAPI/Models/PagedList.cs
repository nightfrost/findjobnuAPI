namespace FindjobnuService.Models
{
    public class PagedList<T> where T : class
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<T> Items { get; set; } = [];
        public PagedList(int totalCount, int pageSize, int currentPage, IEnumerable<T> items)
        {
            TotalCount = totalCount;
            PageSize = pageSize;
            CurrentPage = currentPage;
            Items = items ?? Enumerable.Empty<T>();
        }
    }
}
