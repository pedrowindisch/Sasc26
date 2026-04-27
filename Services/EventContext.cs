using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public class EventContext : IEventContext
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Event? _cachedEvent;

    public EventContext(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public string EventSlug
    {
        get
        {
            var slug = _httpContextAccessor.HttpContext?.Request.RouteValues["eventSlug"] as string;
            if (string.IsNullOrEmpty(slug))
            {
                // Fallback: load default event
                slug = _db.Events.Where(e => e.IsActive).OrderBy(e => e.Id).Select(e => e.Slug).FirstOrDefault() ?? "sasc26";
            }
            return slug;
        }
    }

    public int CurrentEventId => CurrentEvent.Id;

    public Event CurrentEvent
    {
        get
        {
            if (_cachedEvent is not null)
                return _cachedEvent;

            var slug = EventSlug;
            _cachedEvent = _db.Events.AsNoTracking().FirstOrDefault(e => e.Slug == slug && e.IsActive)
                         ?? _db.Events.AsNoTracking().OrderBy(e => e.Id).First()
                         ?? throw new InvalidOperationException("No event configured.");
            return _cachedEvent;
        }
    }
}
