using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Services;

public interface IFeedbackService
{
    Task<FeedbackStatusResult> GetFeedbackStatusAsync(string email);
    Task<FeedbackStatusResult> SubmitFeedbackAsync(SubmitFeedbackDto dto);
    Task<List<LectureFeedbackSummaryDto>> GetLectureFeedbackSummariesAsync();
}

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _db;
    private readonly IEventContext _eventContext;

    public FeedbackService(AppDbContext db, IEventContext eventContext)
    {
        _db = db;
        _eventContext = eventContext;
    }

    private int EventId => _eventContext.CurrentEventId;

    public async Task<FeedbackStatusResult> GetFeedbackStatusAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        var checkedInLectureIds = await _db.CheckIns
            .Where(c => c.EventId == EventId && c.AttendeeEmail == email && c.Status == CheckInStatus.Verified && c.LectureId != null)
            .Select(c => c.LectureId!.Value)
            .Distinct()
            .ToListAsync();

        if (checkedInLectureIds.Count == 0)
        {
            return new FeedbackStatusResult
            {
                Success = true,
                NeedsFeedback = false,
                Message = "Nenhuma palestra assistida para avaliar."
            };
        }

        var alreadyFeedbackLectureIds = await _db.LectureFeedbacks
            .Where(f => f.EventId == EventId && f.AttendeeEmail == email)
            .Select(f => f.LectureId)
            .ToListAsync();

        var pendingLectureIds = checkedInLectureIds.Except(alreadyFeedbackLectureIds).ToList();

        if (pendingLectureIds.Count == 0)
        {
            return new FeedbackStatusResult
            {
                Success = true,
                NeedsFeedback = false,
                Message = "Feedback já registrado."
            };
        }

        var lectures = await _db.Lectures
            .Where(l => l.EventId == EventId && pendingLectureIds.Contains(l.Id))
            .Select(l => new LectureForFeedbackDto
            {
                LectureId = l.Id,
                Title = l.Title,
                Speaker = l.Speaker
            })
            .ToListAsync();

        return new FeedbackStatusResult
        {
            Success = true,
            NeedsFeedback = true,
            Lectures = lectures
        };
    }

    public async Task<FeedbackStatusResult> SubmitFeedbackAsync(SubmitFeedbackDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        foreach (var fb in dto.Feedbacks)
        {
            var existing = await _db.LectureFeedbacks
                .FirstOrDefaultAsync(f => f.EventId == EventId && f.AttendeeEmail == email && f.LectureId == fb.LectureId);
            if (existing is not null)
                continue;

            _db.LectureFeedbacks.Add(new LectureFeedback
            {
                LectureId = fb.LectureId,
                EventId = EventId,
                AttendeeEmail = email,
                Rating = fb.Skipped ? 0 : fb.Rating,
                Comment = fb.Comment?.Trim() ?? string.Empty,
                Skipped = fb.Skipped,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return new FeedbackStatusResult { Success = true, Message = "Feedback registrado com sucesso!" };
    }

    public async Task<List<LectureFeedbackSummaryDto>> GetLectureFeedbackSummariesAsync()
    {
        var lectures = await _db.Lectures.Where(l => l.EventId == EventId).ToListAsync();
        var feedbacks = await _db.LectureFeedbacks.Where(f => f.EventId == EventId).ToListAsync();

        var result = new List<LectureFeedbackSummaryDto>();

        foreach (var lecture in lectures)
        {
            var lectureFeedbacks = feedbacks.Where(f => f.LectureId == lecture.Id).ToList();
            var ratedFeedbacks = lectureFeedbacks.Where(f => !f.Skipped).ToList();
            var skippedCount = lectureFeedbacks.Count(f => f.Skipped);

            double avgRating = ratedFeedbacks.Count > 0
                ? Math.Round(ratedFeedbacks.Average(f => f.Rating), 2)
                : 0;

            result.Add(new LectureFeedbackSummaryDto
            {
                LectureId = lecture.Id,
                Title = lecture.Title,
                Speaker = lecture.Speaker,
                AverageRating = avgRating,
                TotalResponses = lectureFeedbacks.Count,
                TotalSkipped = skippedCount,
                Feedbacks = lectureFeedbacks
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new FeedbackEntryDto
                    {
                        Id = f.Id,
                        AttendeeEmail = f.AttendeeEmail,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        Skipped = f.Skipped,
                        CreatedAt = f.CreatedAt
                    }).ToList()
            });
        }

        return result.Where(r => r.TotalResponses > 0).OrderByDescending(r => r.TotalResponses).ToList();
    }
}
