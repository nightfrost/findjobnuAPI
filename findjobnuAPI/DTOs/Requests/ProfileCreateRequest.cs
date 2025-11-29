namespace FindjobnuService.DTOs.Requests;

using System.ComponentModel.DataAnnotations;

public record ProfileCreateRequest(
    [property: Required] string UserId,
    [property: Required, MaxLength(200)] string FullName,
    [property: EmailAddress] string? Email,
    [property: Phone] string? Phone,
    string? Summary
);
