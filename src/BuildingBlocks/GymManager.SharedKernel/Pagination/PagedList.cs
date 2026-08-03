namespace GymManager.SharedKernel.Pagination;

/// <summary>A page of <typeparamref name="T"/> together with metadata describing the full result set.</summary>
public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedList<T> Empty(int pageNumber, int pageSize) => new([], pageNumber, pageSize, 0);
}
