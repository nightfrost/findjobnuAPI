using FindjobnuService.DTOs.Responses;
using FindjobnuService.Models;
using FindjobnuService.Utilities;

namespace FindjobnuService.Mappers;

public static class JobIndexPostsMapper
{
    public static JobIndexPostResponse ToDto(JobIndexPosts model)
    {
        var banner = WebPConverter.ConvertToWebP(model.BannerPicture);
        var footer = WebPConverter.ConvertToWebP(model.FooterPicture);

        return new JobIndexPostResponse(
            model.JobID,
            model.JobTitle ?? string.Empty,
            model.CompanyName ?? string.Empty,
            model.JobLocation,
            model.JobUrl ?? string.Empty,
            model.Published ?? DateTime.MinValue,
            model.Categories.FirstOrDefault()?.Name,
            string.IsNullOrWhiteSpace(model.JobDescription) ? null : model.JobDescription,
            model.CompanyURL,
            banner.bytes,
            footer.bytes,
            banner.format,
            footer.format,
            banner.mimeType,
            footer.mimeType
        );
    }

    public static PagedResponse<JobIndexPostResponse> ToPagedDto(PagedList<JobIndexPosts> paged)
        => new(
            paged.Items.Select(ToDto).ToList(),
            paged.CurrentPage,
            paged.PageSize,
            paged.TotalCount
        );
}
