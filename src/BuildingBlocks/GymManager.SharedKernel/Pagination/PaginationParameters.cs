namespace GymManager.SharedKernel.Pagination;

/// <summary>Common paging, sorting and free-text search parameters accepted by list endpoints.</summary>
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= MaxPageSize ? value : MaxPageSize;
    }

    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
