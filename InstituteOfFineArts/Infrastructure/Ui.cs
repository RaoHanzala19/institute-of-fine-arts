using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using InstituteOfFineArts.Domain;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InstituteOfFineArts.Infrastructure;

/// <summary>Small presentation helpers shared by all views.</summary>
public static class Ui
{
    public static string Currency { get; set; } = "Rs";

    public static string Money(decimal? value, bool decimals = false)
    {
        if (value is null) return "—";
        var v = value.Value;
        return decimals || v != decimal.Truncate(v) ? $"{Currency} {v:N2}" : $"{Currency} {v:N0}";
    }

    public static string Date(DateTime? d, string format = "dd MMM yyyy") => d?.ToString(format) ?? "—";
    public static string DateTimeText(DateTime? d) => d?.ToString("dd MMM yyyy, hh:mm tt") ?? "—";

    public static string TimeAgo(DateTime when)
    {
        var span = DateTime.Now - when;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} d ago";
        return when.ToString("dd MMM yyyy");
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public static string FileSize(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";

    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<DisplayAttribute>();
        return attr?.GetName() ?? Regex.Replace(value.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
    }

    /// <summary>CSS modifier used by <c>.chip</c> for the given status-like enum.</summary>
    public static string ChipClass(Enum value) => value switch
    {
        SubmissionGrade.Best => "chip-gold",
        SubmissionGrade.Better => "chip-success",
        SubmissionGrade.Good => "chip-info",
        SubmissionGrade.Moderate => "chip-warning",
        SubmissionGrade.Disqualified => "chip-danger",
        SubmissionGrade => "chip-neutral",

        CompetitionStatus.Upcoming => "chip-info",
        CompetitionStatus.Ongoing => "chip-success",
        CompetitionStatus => "chip-neutral",

        ExhibitionStatus.Upcoming => "chip-info",
        ExhibitionStatus.Ongoing => "chip-success",
        ExhibitionStatus.Cancelled => "chip-danger",
        ExhibitionStatus => "chip-neutral",

        SaleStatus.Available => "chip-info",
        SaleStatus.Sold => "chip-success",
        SaleStatus.Returned => "chip-warning",
        SaleStatus => "chip-neutral",

        PaymentStatus.Pending => "chip-warning",
        PaymentStatus.Paid => "chip-success",
        PaymentStatus => "chip-neutral",

        NotificationType.Success => "chip-success",
        NotificationType.Warning => "chip-warning",
        _ => "chip-neutral"
    };

    public static IHtmlContent Chip(this IHtmlHelper html, Enum value, bool dot = true)
    {
        var text = System.Net.WebUtility.HtmlEncode(value.DisplayName());
        var dotHtml = dot ? "<i class=\"chip-dot\"></i>" : "";
        return new HtmlString($"<span class=\"chip {ChipClass(value)}\">{dotHtml}{text}</span>");
    }

    public static IHtmlContent RoleChip(this IHtmlHelper html, string role)
    {
        var cls = role switch { "Admin" => "chip-danger", "Manager" => "chip-gold", "Staff" => "chip-info", _ => "chip-success" };
        return new HtmlString($"<span class=\"chip {cls}\">{System.Net.WebUtility.HtmlEncode(role)}</span>");
    }

    public static IHtmlContent Avatar(this IHtmlHelper html, string? name, string size = "md")
    {
        var hue = Math.Abs((name ?? "?").Aggregate(7, (h, c) => h * 31 + c)) % 360;
        return new HtmlString(
            $"<span class=\"avatar avatar-{size}\" style=\"--h:{hue}\">{System.Net.WebUtility.HtmlEncode(Initials(name))}</span>");
    }

    /// <summary>Round-trips select lists for enum-typed dropdowns.</summary>
    public static IEnumerable<SelectListItem> EnumItems<T>(T? selected = null, params T[] exclude) where T : struct, Enum =>
        Enum.GetValues<T>().Where(v => !exclude.Contains(v))
            .Select(v => new SelectListItem(v.DisplayName(), v.ToString(), selected.HasValue && selected.Value.Equals(v)));

    public static IEnumerable<SelectListItem> EnumItems<T>(T selected, params T[] exclude) where T : struct, Enum =>
        EnumItems<T>((T?)selected, exclude);
    /// <summary>JSON blob consumed by site.js for <c>canvas[data-chart]</c>.</summary>
    public static string ChartJson(string type, InstituteOfFineArts.ViewModels.ChartData data, string? label = null, string? prefix = null) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            type, labels = data.Labels, values = data.Values, label, prefix
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
}
