namespace findjobnuAPI.DTOs;

public record CvReadabilityResult(
    string ExtractedText,
    double ReadabilityScore,
    string Summary
);
