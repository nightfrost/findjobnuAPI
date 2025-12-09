using FindjobnuService.Models;
using System.Text;

namespace JobAgentWorkerService.Templates
{
    public static class EmailTemplates
    {
        public static string JobRecommendationsHtml(string firstName, string frequency, IEnumerable<JobIndexPosts> jobs, string unsubscribeLink)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");
            sb.Append("body{font-family:Segoe UI,Arial,sans-serif;background:#f6f8fb;color:#333;margin:0;padding:0}");
            sb.Append(".container{max-width:720px;margin:0 auto;background:#fff;border-radius:8px;box-shadow:0 1px 4px rgba(0,0,0,.08);overflow:hidden}");
            sb.Append(".header{background:#0f6fff;color:#fff;padding:20px 24px}");
            sb.Append(".content{padding:24px}");
            sb.Append(".job{border-bottom:1px solid #eee;padding:16px 0}");
            sb.Append(".job:last-child{border-bottom:none}");
            sb.Append(".title{font-size:16px;font-weight:600;margin:0}");
            sb.Append(".meta{font-size:13px;color:#666;margin:4px 0 8px}");
            sb.Append(".btn{display:inline-block;background:#0f6fff;color:#fff;text-decoration:none;padding:8px 14px;border-radius:6px;font-size:13px}");
            sb.Append(".footer{padding:16px 24px;color:#999;font-size:12px;background:#fafafa}");
            sb.Append("</style></head><body><div class='container'>");
            sb.Append("<div class='header'><h2 style='margin:0'>Your ").Append(frequency).Append(" job recommendations</h2></div>");
            sb.Append("<div class='content'><p>Hi ").Append(System.Net.WebUtility.HtmlEncode(firstName)).Append(",</p>");
            sb.Append("<p>Here are some new roles you might like:</p>");

            foreach (var job in jobs)
            {
                var title = System.Net.WebUtility.HtmlEncode(job.JobTitle ?? "Job");
                var company = System.Net.WebUtility.HtmlEncode(job.CompanyName ?? "");
                var location = System.Net.WebUtility.HtmlEncode(job.JobLocation ?? "");
                var url = job.JobUrl ?? string.Empty;
                sb.Append("<div class='job'>");
                sb.Append("<p class='title'>").Append(title).Append("</p>");
                sb.Append("<p class='meta'>").Append(company);
                if (!string.IsNullOrWhiteSpace(location)) sb.Append(" · ").Append(location);
                sb.Append("</p>");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    sb.Append("<a class='btn' href='").Append(System.Net.WebUtility.HtmlEncode(url)).Append("' target='_blank' rel='noopener'>View job</a>");
                }
                sb.Append("</div>");
            }

            sb.Append("</div><div class='footer'>");
            sb.Append("You received this email because you subscribed to job recommendations on FindJob.nu. ");
            if (!string.IsNullOrWhiteSpace(unsubscribeLink))
            {
                sb.Append("<a href='").Append(System.Net.WebUtility.HtmlEncode(unsubscribeLink)).Append("'>Unsubscribe</a>.");
            }
            sb.Append("</div></div></body></html>");
            return sb.ToString();
        }
    }
}
