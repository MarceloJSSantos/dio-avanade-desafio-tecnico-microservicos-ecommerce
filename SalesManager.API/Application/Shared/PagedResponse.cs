namespace SalesManager.API.Application.Shared
{
    public class PagedResponse<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalCount)
        {
            this.CurrentPage = pageNumber;
            this.PageSize = pageSize;
            this.TotalCount = totalCount;
            this.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            this.Data = data;
            this.HasPrevious = pageNumber > 1;
            this.HasNext = pageNumber < TotalPages;
        }
    }
}