using Microsoft.AspNetCore.Mvc;
using Sasc26.Models;
using Sasc26.Services;

namespace Sasc26.Controllers;

public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Json(new { success = false, message = "Informe o e-mail." });

        if (dto.Feedbacks == null || dto.Feedbacks.Count == 0)
            return Json(new { success = false, message = "Nenhum feedback enviado." });

        foreach (var fb in dto.Feedbacks)
        {
            if (!fb.Skipped && (fb.Rating < 1 || fb.Rating > 5))
                return Json(new { success = false, message = "Avaliação deve ser entre 1 e 5 estrelas." });
        }

        var result = await _feedbackService.SubmitFeedbackAsync(dto);
        return Json(new { result.Success, result.Message });
    }
}