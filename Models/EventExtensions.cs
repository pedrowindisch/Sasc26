using Microsoft.AspNetCore.Mvc;

namespace Sasc26.Models;

public static class EventExtensions
{
    public static string LogoUrl(this Event? ev, IUrlHelper url)
    {
        if (ev?.LogoImage is { Length: > 0 } && !string.IsNullOrEmpty(ev.LogoContentType))
        {
            return url.Action("Logo", "Home") ?? "/dasc.svg";
        }
        return "/dasc.svg";
    }

    public static string BackgroundDesktopUrl(this Event? ev, IUrlHelper url)
    {
        if (ev?.BackgroundImageDesktop is { Length: > 0 } && !string.IsNullOrEmpty(ev.BackgroundImageDesktopContentType))
        {
            return url.Action("Background", "Home", new { type = "desktop" }) ?? "";
        }
        return "";
    }

    public static string BackgroundMobileUrl(this Event? ev, IUrlHelper url)
    {
        if (ev?.BackgroundImageMobile is { Length: > 0 } && !string.IsNullOrEmpty(ev.BackgroundImageMobileContentType))
        {
            return url.Action("Background", "Home", new { type = "mobile" }) ?? "";
        }
        return ev?.BackgroundDesktopUrl(url) ?? "";
    }
}
