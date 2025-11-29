namespace FindjobnuService.DTOs.Responses;

public record JobIndexPostResponse(
    int Id,
    string Title,
    string Company,
    string? Location,
    string JobUrl,
    DateTime PostedDate,
    string? Category
);
