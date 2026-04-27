using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface IVolunteerService
{
    Task<VolunteerLookupResult> LookupVolunteerAsync(string email);
    Task<VolunteerRegisterResult> RegisterAsync(VolunteerProfileDto dto);
    Task<VolunteerCheckInResult> CheckInAsync(string email);
    Task<VolunteerDashboardDto> GetDashboardAsync(string email);
    Task<List<VolunteerDetailDto>> GetAllVolunteersAsync();
    Task<VolunteerDetailDto?> GetVolunteerByIdAsync(Guid volunteerId);
    Task<VolunteerCheckInResult> AdminAddCheckInAsync(Guid volunteerId, int timeSlotId);
    Task<bool> AdminRemoveCheckInAsync(Guid checkInId);
}

public class VolunteerLookupResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Exists { get; set; }
}

public class VolunteerRegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VolunteerCheckInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VolunteerDashboardDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSlotDto? ActiveTimeSlot { get; set; }
    public bool AlreadyCheckedInCurrentSlot { get; set; }
    public List<VolunteerCheckInEntryDto> CheckIns { get; set; } = [];
}

public class TimeSlotDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
}

public class VolunteerService : IVolunteerService
{
    private static readonly TimeZoneInfo BrasiliaTz = GetBrasiliaTimeZone();

    private readonly AppDbContext _db;
    private readonly EventSettings _settings;
    private readonly IEventContext _eventContext;
    private readonly ILogger<VolunteerService> _logger;

    public VolunteerService(
        AppDbContext db,
        IOptions<EventSettings> settings,
        IEventContext eventContext,
        ILogger<VolunteerService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _eventContext = eventContext;
        _logger = logger;
    }

    private int EventId => _eventContext.CurrentEventId;

    public async Task<VolunteerLookupResult> LookupVolunteerAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        if (!email.EndsWith($"@{_settings.AllowedEmailDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return new VolunteerLookupResult
            {
                Success = false,
                Message = $"Utilize seu e-mail institucional @{_settings.AllowedEmailDomain}."
            };
        }

        var existing = await _db.Volunteers.FirstOrDefaultAsync(v => v.EventId == EventId && v.Email == email);
        if (existing is not null)
        {
            return new VolunteerLookupResult
            {
                Success = true,
                Exists = true,
                Message = "Voluntário encontrado."
            };
        }

        return new VolunteerLookupResult
        {
            Success = true,
            Exists = false,
            Message = "Novo voluntário."
        };
    }

    public async Task<VolunteerRegisterResult> RegisterAsync(VolunteerProfileDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (!email.EndsWith($"@{_settings.AllowedEmailDomain}", StringComparison.OrdinalIgnoreCase))
        {
            return new VolunteerRegisterResult
            {
                Success = false,
                Message = $"Utilize seu e-mail institucional @{_settings.AllowedEmailDomain}."
            };
        }

        var existing = await _db.Volunteers.FirstOrDefaultAsync(v => v.EventId == EventId && v.Email == email);
        if (existing is not null)
        {
            return new VolunteerRegisterResult
            {
                Success = false,
                Message = "Este e-mail já está cadastrado."
            };
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);

        var volunteer = new Volunteer
        {
            Id = Guid.NewGuid(),
            Email = email,
            EventId = EventId,
            Name = dto.Name.Trim(),
            Course = dto.Course,
            Shift = dto.Shift,
            Semester = dto.Semester,
            IsVerified = true,
            RegisteredAt = now
        };

        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync();

        return new VolunteerRegisterResult { Success = true, Message = "Cadastro realizado com sucesso!" };
    }

    public async Task<VolunteerCheckInResult> CheckInAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.EventId == EventId && v.Email == email);
        if (volunteer is null)
            return new VolunteerCheckInResult { Success = false, Message = "Voluntário não encontrado." };

        var activeSlot = await GetActiveTimeSlotAsync();
        if (activeSlot is null)
            return new VolunteerCheckInResult { Success = false, Message = "Não há horário ativo no momento." };

        var alreadyCheckedIn = await _db.VolunteerCheckIns
            .AnyAsync(c => c.EventId == EventId && c.VolunteerId == volunteer.Id && c.TimeSlotId == activeSlot.Id);
        if (alreadyCheckedIn)
            return new VolunteerCheckInResult { Success = false, Message = "Você já registrou presença neste horário." };

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _db.VolunteerCheckIns.Add(new VolunteerCheckIn
        {
            Id = Guid.NewGuid(),
            VolunteerId = volunteer.Id,
            EventId = EventId,
            TimeSlotId = activeSlot.Id,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();

        var label = $"{activeSlot.StartTime:HH:mm} - {activeSlot.EndTime:HH:mm}";
        return new VolunteerCheckInResult { Success = true, Message = $"Presença registrada com sucesso ({label})!" };
    }

    public async Task<VolunteerDashboardDto> GetDashboardAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.EventId == EventId && v.Email == email);
        if (volunteer is null)
            return null!;

        var activeSlot = await GetActiveTimeSlotAsync();
        TimeSlotDto? activeSlotDto = null;
        var alreadyCheckedIn = false;

        if (activeSlot is not null)
        {
            activeSlotDto = new TimeSlotDto
            {
                Id = activeSlot.Id,
                Label = $"{activeSlot.StartTime:HH:mm} - {activeSlot.EndTime:HH:mm}",
                Shift = activeSlot.Shift
            };
            alreadyCheckedIn = await _db.VolunteerCheckIns
                .AnyAsync(c => c.EventId == EventId && c.VolunteerId == volunteer.Id && c.TimeSlotId == activeSlot.Id);
        }

        var checkIns = await _db.VolunteerCheckIns
            .Where(c => c.EventId == EventId && c.VolunteerId == volunteer.Id)
            .Include(c => c.TimeSlot)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new VolunteerCheckInEntryDto
            {
                Id = c.Id,
                TimeSlotId = c.TimeSlotId,
                TimeSlotLabel = $"{c.TimeSlot.StartTime:dd/MM HH:mm} - {c.TimeSlot.EndTime:HH:mm}",
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return new VolunteerDashboardDto
        {
            Name = volunteer.Name,
            Email = volunteer.Email,
            ActiveTimeSlot = activeSlotDto,
            AlreadyCheckedInCurrentSlot = alreadyCheckedIn,
            CheckIns = checkIns
        };
    }

    public async Task<List<VolunteerDetailDto>> GetAllVolunteersAsync()
    {
        return await _db.Volunteers
            .Include(v => v.CheckIns)
            .ThenInclude(c => c.TimeSlot)
            .Where(v => v.EventId == EventId)
            .OrderBy(v => v.Name)
            .Select(v => new VolunteerDetailDto
            {
                Id = v.Id,
                Email = v.Email,
                Name = v.Name,
                Course = v.Course,
                Shift = v.Shift,
                Semester = v.Semester,
                IsVerified = v.IsVerified,
                RegisteredAt = v.RegisteredAt,
                CheckIns = v.CheckIns.Select(c => new VolunteerCheckInEntryDto
                {
                    Id = c.Id,
                    TimeSlotId = c.TimeSlotId,
                    TimeSlotLabel = $"{c.TimeSlot.StartTime:dd/MM HH:mm} - {c.TimeSlot.EndTime:HH:mm}",
                    CreatedAt = c.CreatedAt
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<VolunteerDetailDto?> GetVolunteerByIdAsync(Guid volunteerId)
    {
        var volunteer = await _db.Volunteers
            .Include(v => v.CheckIns)
            .ThenInclude(c => c.TimeSlot)
            .FirstOrDefaultAsync(v => v.EventId == EventId && v.Id == volunteerId);

        if (volunteer is null) return null;

        return new VolunteerDetailDto
        {
            Id = volunteer.Id,
            Email = volunteer.Email,
            Name = volunteer.Name,
            Course = volunteer.Course,
            Shift = volunteer.Shift,
            Semester = volunteer.Semester,
            IsVerified = volunteer.IsVerified,
            RegisteredAt = volunteer.RegisteredAt,
            CheckIns = volunteer.CheckIns.Select(c => new VolunteerCheckInEntryDto
            {
                Id = c.Id,
                TimeSlotId = c.TimeSlotId,
                TimeSlotLabel = $"{c.TimeSlot.StartTime:dd/MM HH:mm} - {c.TimeSlot.EndTime:HH:mm}",
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }

    public async Task<VolunteerCheckInResult> AdminAddCheckInAsync(Guid volunteerId, int timeSlotId)
    {
        var volunteer = await _db.Volunteers.FirstOrDefaultAsync(v => v.EventId == EventId && v.Id == volunteerId);
        if (volunteer is null)
            return new VolunteerCheckInResult { Success = false, Message = "Voluntário não encontrado." };

        var timeSlot = await _db.TimeSlots.FirstOrDefaultAsync(t => t.EventId == EventId && t.Id == timeSlotId);
        if (timeSlot is null)
            return new VolunteerCheckInResult { Success = false, Message = "Horário não encontrado." };

        var already = await _db.VolunteerCheckIns
            .AnyAsync(c => c.EventId == EventId && c.VolunteerId == volunteerId && c.TimeSlotId == timeSlotId);
        if (already)
            return new VolunteerCheckInResult { Success = false, Message = "Check-in já existe para este horário." };

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        _db.VolunteerCheckIns.Add(new VolunteerCheckIn
        {
            Id = Guid.NewGuid(),
            VolunteerId = volunteerId,
            EventId = EventId,
            TimeSlotId = timeSlotId,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();
        return new VolunteerCheckInResult { Success = true, Message = "Check-in adicionado com sucesso." };
    }

    public async Task<bool> AdminRemoveCheckInAsync(Guid checkInId)
    {
        var checkIn = await _db.VolunteerCheckIns.FirstOrDefaultAsync(c => c.EventId == EventId && c.Id == checkInId);
        if (checkIn is null) return false;
        _db.VolunteerCheckIns.Remove(checkIn);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<TimeSlot?> GetActiveTimeSlotAsync()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrasiliaTz);
        return await _db.TimeSlots.FirstOrDefaultAsync(s => s.EventId == EventId && now >= s.StartTime && now <= s.EndTime);
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}
