using Sasc26.Models;

namespace Sasc26.Services;

public interface IEventContext
{
    int CurrentEventId { get; }
    Event CurrentEvent { get; }
    string EventSlug { get; }
}
