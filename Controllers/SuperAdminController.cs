using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sasc26.Data;
using Sasc26.Models;

namespace Sasc26.Controllers;

/// <summary>
/// Super admin controller for managing events (create, edit, list).
/// Accessible at /SuperAdmin (no event slug required).
/// </summary>
public class SuperAdminController : Controller
{
    private readonly AppDbContext _db;

    public SuperAdminController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _db.Events.OrderBy(e => e.Name).ToListAsync();
        return View(events);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Event());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Event ev)
    {
        if (string.IsNullOrWhiteSpace(ev.Slug) || string.IsNullOrWhiteSpace(ev.Name))
        {
            ModelState.AddModelError("", "Slug e Nome são obrigatórios.");
            return View(ev);
        }

        if (await _db.Events.AnyAsync(e => e.Slug == ev.Slug))
        {
            ModelState.AddModelError("", "Já existe um evento com este slug.");
            return View(ev);
        }

        ev.AdminEmailsJson = string.IsNullOrWhiteSpace(ev.AdminEmailsJson) ? "[]" : ev.AdminEmailsJson;
        ev.PostCheckinButtonsJson = string.IsNullOrWhiteSpace(ev.PostCheckinButtonsJson) ? "[]" : ev.PostCheckinButtonsJson;

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        return View(ev);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Event ev)
    {
        var existing = await _db.Events.FindAsync(ev.Id);
        if (existing is null) return NotFound();

        existing.Slug = ev.Slug;
        existing.Name = ev.Name;
        existing.Subtitle = ev.Subtitle;
        existing.AllowedEmailDomain = ev.AllowedEmailDomain;
        existing.InstagramUrl = ev.InstagramUrl;
        existing.TshirtPresaleUrl = ev.TshirtPresaleUrl;
        existing.AdminEmailsJson = ev.AdminEmailsJson;
        existing.PostCheckinButtonsJson = ev.PostCheckinButtonsJson;
        existing.PrimaryColor = ev.PrimaryColor;
        existing.AccentColor = ev.AccentColor;
        existing.BackgroundColor = ev.BackgroundColor;
        existing.TextColor = ev.TextColor;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
