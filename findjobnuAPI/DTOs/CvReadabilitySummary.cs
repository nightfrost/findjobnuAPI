namespace FindjobnuService.DTOs;

public record CvReadabilitySummary(
    int TotalChars,
    int TotalWords,
    int TotalLines,
    bool HasEmail,
    bool HasPhone,
    int BulletCount,
    int MatchedSections,
    int TotalSectionKeywords,
    string? Note
);
