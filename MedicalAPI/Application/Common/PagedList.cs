namespace MedicalAPI.Application.Common;

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PagedList<T> Create(IReadOnlyList<T> source, int pageNumber, int pageSize)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = source.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)safePageSize);
        var items = source
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new(
            items,
            safePageNumber,
            safePageSize,
            totalItems,
            totalPages,
            safePageNumber > 1 && totalPages > 0,
            safePageNumber < totalPages);
    }
}
