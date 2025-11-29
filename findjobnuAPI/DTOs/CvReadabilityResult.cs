namespace FindjobnuService.DTOs;

public record CvReadabilityResult(
    string ExtractedText,
    double ReadabilityScore,
    CvReadabilitySummary Summary
);
