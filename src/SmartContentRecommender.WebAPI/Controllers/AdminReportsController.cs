using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SmartContentRecommender.Application.Analytics.Interfaces;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AdminReportsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _analyticsService.BuildDefenseReportAsync(fromUtc, toUtc, 10, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Section,Field,Value");
        sb.AppendLine($"Summary,GeneratedAtUtc,{Escape(report.GeneratedAtUtc.ToString("O"))}");
        sb.AppendLine($"Summary,UsersCount,{report.UsersCount}");
        sb.AppendLine($"Summary,ActionsCount,{report.ActionsCount}");

        foreach (var user in report.Users)
        {
            sb.AppendLine($"User,UserId,{user.UserId}");
            sb.AppendLine($"User,Email,{Escape(user.Email)}");
            sb.AppendLine($"User,Role,{Escape(user.Role)}");
            sb.AppendLine($"User,IsBlocked,{user.IsBlocked}");
            sb.AppendLine($"User,ActionsCount,{user.ActionsCount}");
        }

        foreach (var action in report.RecentActions)
        {
            sb.AppendLine($"Action,UserEmail,{Escape(action.UserEmail)}");
            sb.AppendLine($"Action,ContentTitle,{Escape(action.ContentTitle)}");
            sb.AppendLine($"Action,Type,{Escape(action.ActionType)}");
            sb.AppendLine($"Action,CreatedAtUtc,{Escape(action.CreatedAtUtc.ToString("O"))}");
        }

        foreach (var metric in report.RecommendationMetrics)
        {
            sb.AppendLine($"Metric,UserId,{metric.UserId}");
            sb.AppendLine($"Metric,Algorithm,{Escape(metric.Algorithm)}");
            sb.AppendLine($"Metric,K,{metric.K}");
            sb.AppendLine($"Metric,PrecisionAtK,{metric.PrecisionAtK:F4}");
            sb.AppendLine($"Metric,RecallAtK,{metric.RecallAtK:F4}");
            sb.AppendLine($"Metric,NdcgAtK,{metric.NdcgAtK:F4}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"defense-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _analyticsService.BuildDefenseReportAsync(fromUtc, toUtc, 10, cancellationToken);

        using var stream = new MemoryStream();
        using (var document = new PdfDocument())
        {
            document.Info.Title = "Smart Content Recommender - Defense Report";
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
            var font = new XFont("Arial", 10, XFontStyle.Regular);

            var y = 30;
            gfx.DrawString("Defense Report", titleFont, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 30;
            gfx.DrawString($"Generated: {report.GeneratedAtUtc:O}", font, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 18;
            gfx.DrawString($"Users: {report.UsersCount}", font, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 18;
            gfx.DrawString($"Actions: {report.ActionsCount}", font, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 24;

            gfx.DrawString("Top users:", titleFont, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 20;

            foreach (var user in report.Users.Take(12))
            {
                if (y > page.Height - 30)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 30;
                }

                var line = $"{user.Email} | Role: {user.Role} | Blocked: {user.IsBlocked} | Actions: {user.ActionsCount}";
                gfx.DrawString(line, font, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
                y += 16;
            }

            y += 8;
            gfx.DrawString("Recommendation quality (KNN):", titleFont, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
            y += 20;

            foreach (var metric in report.RecommendationMetrics)
            {
                if (y > page.Height - 30)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 30;
                }

                var line = $"User: {metric.UserId} | P@K={metric.PrecisionAtK:F3} | R@K={metric.RecallAtK:F3} | NDCG@K={metric.NdcgAtK:F3}";
                gfx.DrawString(line, font, XBrushes.Black, new XRect(30, y, page.Width - 60, 20), XStringFormats.TopLeft);
                y += 16;
            }

            document.Save(stream, false);
        }

        var fileName = $"defense-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
        return File(stream.ToArray(), "application/pdf", fileName);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}

