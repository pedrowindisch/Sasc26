using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Sasc26.Services;

/// <summary>
/// Helper methods for event-slug-aware URL generation and redirects.
/// </summary>
public static class EventHelper
{
    /// <summary>
    /// Gets the event slug from the current HTTP context route values.
    /// </summary>
    public static string GetEventSlug(HttpContext? httpContext)
    {
        return httpContext?.Request.RouteValues["eventSlug"] as string ?? string.Empty;
    }

    /// <summary>
    /// Returns a redirect to the event-scoped URL: /{slug}/{path}
    /// If no slug is available, redirects to the default route.
    /// </summary>
    public static IActionResult RedirectToEvent(string slug, string path)
    {
        if (string.IsNullOrEmpty(slug))
            return new RedirectResult($"/{path}");
        return new RedirectResult($"/{slug}/{path}");
    }

    /// <summary>
    /// Generates an event-scoped URL: /{slug}/{path}
    /// </summary>
    public static string EventUrl(string slug, string path)
    {
        if (string.IsNullOrEmpty(slug))
            return $"/{path}";
        return $"/{slug}/{path}";
    }
}
