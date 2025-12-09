namespace FindjobnuService.DTOs.Requests;

using FindjobnuService.Models;
using System.ComponentModel.DataAnnotations;

public record JobAgentUpdateRequest(
    [property: Required] bool Enabled,
    JobAgentFrequency? Frequency
);
