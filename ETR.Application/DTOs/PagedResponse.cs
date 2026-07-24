namespace ETR.Application.DTOs;

public record PagedResponse<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
