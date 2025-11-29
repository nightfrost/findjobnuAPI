namespace FindjobnuService.DTOs.Responses;

using System.Collections.Generic;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
