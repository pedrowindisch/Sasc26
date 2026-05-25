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
            return slug ?? string.Empty;
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
            if (string.IsNullOrEmpty(slug))
            {
                // No event slug in route - this shouldn't happen for event-scoped pages
                throw new InvalidOperationException("No event slug specified in the URL.");
            }

            _cachedEvent = _db.Events.AsNoTracking().FirstOrDefault(e => e.Slug == slug && e.IsActive);
            if (_cachedEvent is null)
            {
                throw new KeyNotFoundException($"Event with slug '{slug}' was not found or is not active.");
            }
            return _cachedEvent;
        }
    }
}
