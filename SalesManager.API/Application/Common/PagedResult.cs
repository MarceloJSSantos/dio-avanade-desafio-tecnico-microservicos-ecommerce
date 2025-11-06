namespace SalesManager.API.Application.Common
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }

        public PagedResult(IEnumerable<T> items, int totalCount)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
        }

        public PagedResult() { }
    }
}