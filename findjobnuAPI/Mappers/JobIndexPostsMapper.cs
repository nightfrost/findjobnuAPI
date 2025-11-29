using FindjobnuService.DTOs.Responses;
using FindjobnuService.Models;

namespace FindjobnuService.Mappers;

public static class JobIndexPostsMapper
{
    public static JobIndexPostResponse ToDto(JobIndexPosts model) => new(
        model.JobID,
        model.JobTitle ?? string.Empty,
        model.CompanyName ?? string.Empty,
        model.JobLocation,
        model.JobUrl ?? string.Empty,
        model.Published ?? DateTime.MinValue,
        model.Categories.FirstOrDefault()?.Name
    );

    public static PagedResponse<JobIndexPostResponse> ToPagedDto(PagedList<JobIndexPosts> paged)
        => new(
            paged.Items.Select(ToDto).ToList(),
            paged.CurrentPage,
            paged.PageSize,
            paged.TotalCount
        );
}
