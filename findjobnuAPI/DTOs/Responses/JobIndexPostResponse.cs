namespace FindjobnuService.DTOs.Responses;

public record JobIndexPostResponse(
    int Id,
    string Title,
    string Company,
    string? Location,
    string JobUrl,
    DateTime PostedDate,
    string? Category,
    string? Description,
    string? CompanyUrl,
    byte[]? BannerPicture,
    byte[]? FooterPicture,
    List<string>? Keywords,
    string? BannerFormat,
    string? FooterFormat,
    string? BannerMimeType,
    string? FooterMimeType
);
