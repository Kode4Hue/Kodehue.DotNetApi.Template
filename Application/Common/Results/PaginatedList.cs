namespace Application.Common.Results
{
    public sealed class PaginatedList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }

        private PaginatedList(IReadOnlyList<T> items, int pageNumber, int totalPages, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            TotalPages = totalPages;
            TotalCount = totalCount;
        }

        public static PaginatedList<T> Create(IEnumerable<T> items, int pageNumber, int totalPages, int totalCount)
        {
            var itemList = items is IReadOnlyList<T> readOnly ? readOnly : items.ToList();
            return new PaginatedList<T>(itemList, pageNumber, totalPages, totalCount);
        }
    }
}
