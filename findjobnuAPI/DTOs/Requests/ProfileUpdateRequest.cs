namespace FindjobnuService.DTOs.Requests;

using System.ComponentModel.DataAnnotations;

public record ProfileUpdateRequest(
    [property: Required] string UserId,
    [property: MaxLength(200)] string? FullName,
    [property: EmailAddress] string? Email,
    [property: Phone] string? Phone,
    string? Summary
);
