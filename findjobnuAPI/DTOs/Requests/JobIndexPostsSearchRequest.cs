namespace FindjobnuService.DTOs.Requests;

using System.ComponentModel.DataAnnotations;

public record JobIndexPostsSearchRequest(
    [property: MaxLength(200)] string? SearchTerm,
    [property: MaxLength(100)] string? Location,
    [property: MaxLength(100)] string? Category,
    DateTime? PostedAfter,
    DateTime? PostedBefore,
    [property: Range(1, int.MaxValue)] int Page = 1
);
