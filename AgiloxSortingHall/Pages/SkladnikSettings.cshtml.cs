using AgiloxSortingHall.Data;
using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AgiloxSortingHall.Pages;

public class SkladnikSettingsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<SkladnikSettingsModel> _logger;

    public SkladnikSettingsModel(AppDbContext db, ILogger<SkladnikSettingsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<HallRow> Rows { get; set; } = new();

    public RowSelectionStrategy CurrentStrategy { get; set; } = RowSelectionStrategy.MostFreePallets;

    [BindProperty] public Dictionary<int, string?> RowName { get; set; } = new();
    [BindProperty] public Dictionary<int, string?> RowColor { get; set; } = new();
    [BindProperty] public Dictionary<int, int> RowCapacity { get; set; } = new();

    [TempData] public string? ErrorMessage { get; set; }
    [TempData] public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Rows = await _db.HallRows
            .Include(r => r.Slots)
            .OrderBy(r => r.Name)
            .ToListAsync();

        foreach (var r in Rows)
        {
            RowName[r.Id] = r.Name;
            RowColor[r.Id] = r.ColorHex;
            RowCapacity[r.Id] = r.Capacity;
        }

        var settings = await _db.HallSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new HallSettings { Id = 1, RowSelectionStrategy = RowSelectionStrategy.MostFreePallets };
            _db.HallSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        CurrentStrategy = settings.RowSelectionStrategy;
    }

    // --- Strategie ---
    public async Task<IActionResult> OnPostSetRowSelectionStrategyAsync(RowSelectionStrategy strategy)
    {
        var settings = await _db.HallSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new HallSettings { Id = 1 };
            _db.HallSettings.Add(settings);
        }

        settings.RowSelectionStrategy = strategy;
        await _db.SaveChangesAsync();

        SuccessMessage = "Strategie byla uložena.";
        return RedirectToPage();
    }

    // --- Pøidat øadu ---
    public async Task<IActionResult> OnPostAddRowAsync()
    {
        // jednoduchý default: další èíslo podle Id / poètu
        var existing = await _db.HallRows.ToListAsync();
        var nextIndex = existing.Count + 1;

        var newRow = new HallRow
        {
            Name = $"Øada{nextIndex}",
            ColorHex = "#0d6efd",
            Capacity = existing.Any() ? existing.Max(r => r.Capacity) : 10,
            Article = ""
        };

        _db.HallRows.Add(newRow);
        await _db.SaveChangesAsync();

        // vytvoøíme sloty
        var slots = Enumerable.Range(0, newRow.Capacity)
            .Select(i => new PalletSlot
            {
                HallRowId = newRow.Id,
                PositionIndex = i,
                State = PalletState.Empty
            })
            .ToList();

        _db.PalletSlots.AddRange(slots);
        await _db.SaveChangesAsync();

        SuccessMessage = $"Øada '{newRow.Name}' byla pøidána.";
        return RedirectToPage();
    }

    // --- Smazat øadu ---
    public async Task<IActionResult> OnPostDeleteRowAsync(int rowId)
    {
        var row = await _db.HallRows
            .Include(r => r.Slots)
            .FirstOrDefaultAsync(r => r.Id == rowId);

        if (row == null)
        {
            ErrorMessage = "Øada neexistuje.";
            return RedirectToPage();
        }

        // bezpeènost: nesmazat, pokud má obsazené sloty
        if (row.Slots.Any(s => s.State == PalletState.Occupied))
        {
            ErrorMessage = $"Øadu '{row.Name}' nelze smazat – obsahuje obsazené sloty.";
            return RedirectToPage();
        }

        // pøípadnì: pokud chceš blokovat i pøi pending call
        var hasPending = await _db.RowCalls.AnyAsync(c => c.HallRowId == rowId && c.Status == RowCallStatus.Pending);
        if (hasPending)
        {
            ErrorMessage = $"Øadu '{row.Name}' nelze smazat – existují pending požadavky.";
            return RedirectToPage();
        }

        _db.PalletSlots.RemoveRange(row.Slots);
        _db.HallRows.Remove(row);
        await _db.SaveChangesAsync();

        SuccessMessage = $"Øada '{row.Name}' byla smazána.";
        return RedirectToPage();
    }

    // --- Uložit øady (název/barva/kapacita) ---
    public async Task<IActionResult> OnPostSaveRowsAsync(int? saveSingleRowId)
    {
        // natáhneme všechny, nebo jen jednu
        var query = _db.HallRows.Include(r => r.Slots).AsQueryable();
        if (saveSingleRowId.HasValue)
            query = query.Where(r => r.Id == saveSingleRowId.Value);

        var rows = await query.ToListAsync();

        foreach (var row in rows)
        {
            // name
            if (RowName.TryGetValue(row.Id, out var newName) && !string.IsNullOrWhiteSpace(newName))
                row.Name = newName.Trim();

            // color
            if (RowColor.TryGetValue(row.Id, out var newColor) && !string.IsNullOrWhiteSpace(newColor))
                row.ColorHex = newColor.Trim();

            // capacity
            if (RowCapacity.TryGetValue(row.Id, out var newCap))
            {
                if (newCap < 0) newCap = 0;

                if (newCap != row.Capacity)
                {
                    var ok = await ApplyCapacityChangeAsync(row, newCap);
                    if (!ok)
                    {
                        // ApplyCapacityChangeAsync nastaví ErrorMessage
                        return RedirectToPage();
                    }
                }
            }
        }

        await _db.SaveChangesAsync();

        SuccessMessage = saveSingleRowId.HasValue ? "Øada byla uložena." : "Øady byly uloženy.";
        return RedirectToPage();
    }

    private async Task<bool> ApplyCapacityChangeAsync(HallRow row, int newCapacity)
    {
        var oldCapacity = row.Capacity;

        // snížení kapacity: nesmí “odøíznout” obsazený slot
        if (newCapacity < oldCapacity)
        {
            // sloty s PositionIndex >= newCapacity by zmizely
            var wouldRemove = row.Slots.Where(s => s.PositionIndex >= newCapacity).ToList();
            if (wouldRemove.Any(s => s.State == PalletState.Occupied))
            {
                ErrorMessage = $"Nelze snížit kapacitu '{row.Name}' na {newCapacity} – nad novou kapacitou jsou obsazené sloty.";
                return false;
            }

            _db.PalletSlots.RemoveRange(wouldRemove);
            row.Capacity = newCapacity;
            return true;
        }

        // zvýšení kapacity: pøidáme nové sloty
        if (newCapacity > oldCapacity)
        {
            var existingIndexes = row.Slots.Select(s => s.PositionIndex).ToHashSet();

            var toAdd = new List<PalletSlot>();
            for (int i = 0; i < newCapacity; i++)
            {
                if (!existingIndexes.Contains(i))
                {
                    toAdd.Add(new PalletSlot
                    {
                        HallRowId = row.Id,
                        PositionIndex = i,
                        State = PalletState.Empty
                    });
                }
            }

            _db.PalletSlots.AddRange(toAdd);
            row.Capacity = newCapacity;
            return true;
        }

        return true;
    }
}
