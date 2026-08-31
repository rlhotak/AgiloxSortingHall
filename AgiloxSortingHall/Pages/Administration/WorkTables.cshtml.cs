using AgiloxSortingHall.Data;
using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace AgiloxSortingHall.Pages.Administration;

public class WorkTablesModel : PageModel
{
    private readonly AppDbContext _db;

    public WorkTablesModel(AppDbContext db)
    {
        _db = db;
    }

    public List<WorkTable> Tables { get; private set; } = new();

    public IReadOnlyList<WorkTableCategory> CategoryOptions { get; } =
        Enum.GetValues(typeof(WorkTableCategory))
            .Cast<WorkTableCategory>()
            .OrderBy(x => (int)x)
            .ToList();

    [BindProperty]
    public EditVm Edit { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tables = await _db.WorkTables
            .AsNoTracking()
            .OrderBy(t => t.Category)
            .ThenBy(t => t.DisplayName)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Edit.InputStationName))
            ModelState.AddModelError("", "InputStationName je povinné.");

        if (string.IsNullOrWhiteSpace(Edit.OutputStationName))
            ModelState.AddModelError("", "OutputStationName je povinné.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var entity = new WorkTable
        {
            DisplayName = string.IsNullOrWhiteSpace(Edit.DisplayName) ? null : Edit.DisplayName.Trim(),
            InputStationName = Edit.InputStationName.Trim(),
            OutputStationName = Edit.OutputStationName.Trim(),
            Category = Edit.Category
        };

        _db.WorkTables.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Flash"] = "Stùl byl vytvoøen.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (Edit.Id <= 0)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(Edit.InputStationName))
            ModelState.AddModelError("", "InputStationName je povinné.");

        if (string.IsNullOrWhiteSpace(Edit.OutputStationName))
            ModelState.AddModelError("", "OutputStationName je povinné.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var entity = await _db.WorkTables.FirstOrDefaultAsync(x => x.Id == Edit.Id);
        if (entity == null)
            return NotFound();

        entity.DisplayName = string.IsNullOrWhiteSpace(Edit.DisplayName) ? null : Edit.DisplayName.Trim();
        entity.InputStationName = Edit.InputStationName.Trim();
        entity.OutputStationName = Edit.OutputStationName.Trim();
        entity.Category = Edit.Category;

        await _db.SaveChangesAsync();

        TempData["Flash"] = "Zmìny byly uloženy.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var entity = await _db.WorkTables.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return NotFound();

        _db.WorkTables.Remove(entity);
        await _db.SaveChangesAsync();

        TempData["Flash"] = "Stùl byl smazán.";
        return RedirectToPage();
    }

    public class EditVm
    {
        public int Id { get; set; }
        public string? DisplayName { get; set; }
        public string InputStationName { get; set; } = "";
        public string OutputStationName { get; set; } = "";
        public WorkTableCategory Category { get; set; } = WorkTableCategory.Unknown;
    }
}
