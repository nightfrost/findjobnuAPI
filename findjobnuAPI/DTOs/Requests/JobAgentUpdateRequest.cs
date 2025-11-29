namespace FindjobnuService.DTOs.Requests;

using System.ComponentModel.DataAnnotations;
using FindjobnuService.Models;

public record JobAgentUpdateRequest(
    [property: Required] bool Enabled,
    JobAgentFrequency? Frequency
);
