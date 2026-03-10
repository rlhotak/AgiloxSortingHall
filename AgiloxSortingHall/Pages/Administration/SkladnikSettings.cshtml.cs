using System.Text.Json;
using AgiloxSortingHall.Data;
using AgiloxSortingHall.Dto;
using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AgiloxSortingHall.Pages.Administration;

/// <summary>
/// Stránka nastavení pro skladníka.
/// Umožòuje:
/// - zvolit strategii výbìru øady (prioritizace)
/// - spravovat øady (název, barva, kapacita, mazání, pøidání)
/// - nastavit "název oblasti" (Agilox stationarea), která urèuje limit poètu stanic/øad
///   dle reálného poètu pozic vrácených z Agilox endpointu /stationarea.
/// </summary>
[Authorize]
public class SkladnikSettingsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<SkladnikSettingsModel> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// JSON options pro deserializaci odpovìdí z Agiloxu.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Vytvoøí page model a pøipraví DB + logging + HttpClientFactory.
    /// 
    /// Používá pojmenovaný HttpClient "Agilox", který je nakonfigurovaný v Program.cs
    /// (BaseAddress se bere z appsettings.json: Agilox:BaseUrl).
    /// </summary>
    public SkladnikSettingsModel(
        AppDbContext db,
        ILogger<SkladnikSettingsModel> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Seznam øad v hale (vèetnì slotù) pro zobrazení i validace.
    /// </summary>
    public List<HallRow> Rows { get; set; } = new();

    /// <summary>
    /// Aktuálnì zvolená strategie výbìru øady.
    /// </summary>
    public RowSelectionStrategy CurrentStrategy { get; set; } = RowSelectionStrategy.MostFreePallets;

    /// <summary>
    /// Bindnuté názvy øad z formuláøe: RowName[RowId] = "Název".
    /// </summary>
    [BindProperty]
    public Dictionary<int, string?> RowName { get; set; } = new();

    /// <summary>
    /// Bindnuté barvy øad z formuláøe: RowColor[RowId] = "#RRGGBB".
    /// </summary>
    [BindProperty]
    public Dictionary<int, string?> RowColor { get; set; } = new();

    /// <summary>
    /// Bindnuté kapacity øad z formuláøe: RowCapacity[RowId] = kapacita.
    /// </summary>
    [BindProperty]
    public Dictionary<int, int> RowCapacity { get; set; } = new();

    /// <summary>
    /// Chybová hláška zobrazovaná po redirectu.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Úspìšná hláška zobrazovaná po redirectu.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    // ---------------------------
    // Agilox stationarea / limit
    // ---------------------------

    /// <summary>
    /// Název oblasti (stationarea) naètený z DB jako aktuálnì uložená hodnota.
    /// </summary>
    public string StationAreaNameCurrent { get; set; } = "Hotovo";

    /// <summary>
    /// Bindnutá hodnota z formuláøe (editovatelný input pro skladníka).
    /// </summary>
    [BindProperty]
    public string? StationAreaName { get; set; }

    /// <summary>
    /// Názvy dostupných oblastí z Agiloxu (kvùli našeptávaèi a validaci).
    /// </summary>
    public List<string> AvailableStationAreas { get; set; } = new();

    /// <summary>
    /// Maximální poèet pozic/stanic pro vybranou oblast dle Agiloxu (stationarea.count).
    /// Pokud je null, nepodaøilo se Agilox naèíst a limit se neuplatní.
    /// </summary>
    public int? MaxStationsForArea { get; set; }

    /// <summary>
    /// Aktuální poèet definovaných pozic v aplikaci (souèet kapacit všech øad).
    /// To odpovídá reálnému "poètu slotù", které aplikace umožní používat.
    /// </summary>
    public int CurrentPositionsCount => Rows?.Sum(r => r.Capacity) ?? 0;

    /// <summary>
    /// Zbývající volné pozice do limitu (pokud limit známe).
    /// </summary>
    public int? RemainingPositions =>
        MaxStationsForArea.HasValue ? Math.Max(0, MaxStationsForArea.Value - CurrentPositionsCount) : null;

    /// <summary>
    /// Urèuje, zda lze pøidat další øadu.
    /// Pøi neznámém limitu povolíme (fail-open).
    /// Pøi známém limitu povolíme, pokud existuje alespoò 1 volná pozice.
    /// </summary>
    public bool CanAddRow => MaxStationsForArea is null || CurrentPositionsCount < MaxStationsForArea;


    /// <summary>
    /// Naète stránku (GET) a pøipraví všechna data pro zobrazení.
    /// </summary>
    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    /// <summary>
    /// Naète:
    /// - øady a jejich sloty z DB
    /// - globální nastavení haly (vèetnì stationarea)
    /// - limit poètu stanic/øad z Agiloxu podle stationarea.count
    /// </summary>
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
            // inicializace nastavení – bezpeèné defaulty
            settings = new HallSettings
            {
                Id = 1,
                RowSelectionStrategy = RowSelectionStrategy.MostFreePallets,
                StationAreaName = "Hotovo"
            };
            _db.HallSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        CurrentStrategy = settings.RowSelectionStrategy;

        StationAreaNameCurrent = string.IsNullOrWhiteSpace(settings.StationAreaName)
            ? "Hotovo"
            : settings.StationAreaName.Trim();

        StationAreaName = StationAreaNameCurrent;

        MaxStationsForArea = await GetMaxStationsForAreaAsync(StationAreaNameCurrent);
    }

    // ---------------------------
    // Agilox stationarea helpers
    // ---------------------------

    /// <summary>
    /// Vytvoøí pojmenovaného HTTP klienta pro Agilox API.
    /// 
    /// Poznámka:
    /// - "Agilox" klient je registrován v Program.cs pøes AddHttpClient("Agilox", ...).
    /// - BaseAddress se bere z appsettings.json (Agilox:BaseUrl).
    /// </summary>
    private HttpClient CreateAgiloxClient()
        => _httpClientFactory.CreateClient("Agilox");

    /// <summary>
    /// Naète JSON z endpointu Agiloxu GET /stationarea a vrátí pouze položky,
    /// které reprezentují skuteèné stationarea objekty.
    ///
    /// Endpoint mùže obsahovat i technické položky (napø. "version": 1109),
    /// které nejsou objekt typu StationAreaDto. Tyto položky se ignorují.
    /// </summary>
    private async Task<Dictionary<string, StationAreaDto>?> FetchStationAreasAsync()
    {
        try
        {
            var http = CreateAgiloxClient();

            var json = await http.GetStringAsync("stationarea");

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var result = new Dictionary<string, StationAreaDto>(StringComparer.Ordinal);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                // /stationarea vrací i meta klíèe typu "version": 1109 (number) -> ignorovat
                if (prop.Value.ValueKind != JsonValueKind.Object)
                    continue;

                // Pokus o deserializaci hodnoty na StationAreaDto
                // (kdyby se nìkde objevilo nìco divného, radši to jen pøeskoèíme)
                try
                {
                    var dto = prop.Value.Deserialize<StationAreaDto>(_jsonOptions);
                    if (dto != null)
                        result[prop.Name] = dto;
                }
                catch (JsonException)
                {
                    // ignorujeme konkrétní rozbitou položku a pokraèujeme dál
                    continue;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nepodaøilo se naèíst stationarea z Agiloxu (GET /stationarea).");
            return null;
        }
    }


    /// <summary>
    /// Vrátí maximální poèet "stanic/pozic" pro danou oblast dle Agiloxu.
    /// Požadavek: uživatel nesmí pøidat více øad, než reálnì existuje v Agiloxu.
    /// 
    /// Využívá hodnotu stationarea.count.
    /// </summary>
    private async Task<int?> GetMaxStationsForAreaAsync(string areaName)
    {
        var dict = await FetchStationAreasAsync();
        if (dict == null) return null;

        // uložíme si názvy pro UI (datalist)
        AvailableStationAreas = dict.Keys.OrderBy(x => x).ToList();

        if (!dict.TryGetValue(areaName, out var area))
            return null;

        // limit podle "count" (poèet pozic v dané stationarea)
        return area.Count;

        // pokud chceme zohlednit blokované pozice:
        // return Math.Max(0, area.Count - area.CountBlocked);
    }

    // ---------------------------
    // Strategie
    // ---------------------------

    /// <summary>
    /// Uloží vybranou strategii výbìru øady do globálního nastavení haly.
    /// </summary>
    public async Task<IActionResult> OnPostSetRowSelectionStrategyAsync(RowSelectionStrategy strategy)
    {
        var settings = await _db.HallSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new HallSettings { Id = 1, StationAreaName = "Hotovo" };
            _db.HallSettings.Add(settings);
        }

        settings.RowSelectionStrategy = strategy;
        await _db.SaveChangesAsync();

        SuccessMessage = "Strategie byla uložena.";
        return RedirectToPage();
    }

    // ---------------------------
    // Název oblasti (stationarea)
    // ---------------------------

    /// <summary>
    /// Uloží název oblasti (stationarea) do DB.
    /// Validuje proti Agilox /stationarea, aby si skladník nenastavil neexistující oblast.
    /// </summary>
    public async Task<IActionResult> OnPostSetStationAreaNameAsync()
    {
        var settings = await _db.HallSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new HallSettings { Id = 1, RowSelectionStrategy = RowSelectionStrategy.MostFreePallets };
            _db.HallSettings.Add(settings);
        }

        var newName = (StationAreaName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(newName))
            newName = "Hotovo";

        var dict = await FetchStationAreasAsync();
        if (dict == null)
        {
            // Fail-open: Agilox nedostupný, uložíme bez ovìøení.
            settings.StationAreaName = newName;
            await _db.SaveChangesAsync();

            SuccessMessage = "Název oblasti byl uložen (Agilox nebyl dostupný pro ovìøení).";
            return RedirectToPage();
        }

        if (!dict.ContainsKey(newName))
        {
            ErrorMessage = $"Oblast '{newName}' neexistuje v Agiloxu. Zkontroluj název.";
            return RedirectToPage();
        }

        settings.StationAreaName = newName;
        await _db.SaveChangesAsync();

        SuccessMessage = $"Oblast byla nastavena na '{newName}'.";
        return RedirectToPage();
    }

    // ---------------------------
    // Øady / stanice
    // ---------------------------

    /// <summary>
    /// Pøidá novou øadu do DB a vytvoøí k ní odpovídající sloty.
    /// 
    /// Zároveò kontroluje limit poètu øad podle Agilox stationarea.count,
    /// aby nešlo pøidat víc stanic, než reálnì existuje v Agilox systému.
    /// </summary>
    public async Task<IActionResult> OnPostAddRowAsync()
    {
        // Naèteme aktuálnì nastavenou oblast z DB (fallback "Hotovo")
        var settings = await _db.HallSettings.FirstOrDefaultAsync();
        var areaName = settings?.StationAreaName?.Trim();
        if (string.IsNullOrWhiteSpace(areaName)) areaName = "Hotovo";

        // Natáhneme existující øady (staèí Capacity)
        var existing = await _db.HallRows.AsNoTracking().ToListAsync();
        var currentPositions = existing.Sum(r => r.Capacity);

        // Limit z Agiloxu (stationarea.count = poèet pozic v oblasti)
        var maxPositions = await GetMaxStationsForAreaAsync(areaName);

        // Default kapacita nové øady (stejnì jako døív)
        var defaultCapacity = existing.Any() ? existing.Max(r => r.Capacity) : 10;

        // Hard blokace: nesmíme pøekroèit poèet pozic v Agiloxu
        if (maxPositions.HasValue && (currentPositions + defaultCapacity) > maxPositions.Value)
        {
            var free = Math.Max(0, maxPositions.Value - currentPositions);
            ErrorMessage =
                $"Nelze pøidat øadu – oblast '{areaName}' má v Agiloxu max {maxPositions.Value} pozic. " +
                $"Aktuálnì je nastaveno {currentPositions} pozic, volných zbývá {free}.";
            return RedirectToPage();
        }


        // jednoduchý default: další èíslo podle poètu existujících øad
        var nextIndex = existing.Count + 1;

        var newRow = new HallRow
        {
            Name = $"Øada{nextIndex}",
            ColorHex = "#0d6efd",
            Capacity = defaultCapacity,
            Article = ""
        };

        _db.HallRows.Add(newRow);
        await _db.SaveChangesAsync();

        // vytvoøíme sloty pro øadu podle kapacity
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

    /// <summary>
    /// Smaže øadu, pokud:
    /// - nemá obsazené sloty
    /// - nemá pending požadavky (RowCalls)
    /// </summary>
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

        // blokace pøi pending call
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

    /// <summary>
    /// Uloží zmìny øad (název/barva/kapacita).
    /// Pokud je zadán <paramref name="saveSingleRowId"/>, uloží pouze jednu øadu.
    /// </summary>
    public async Task<IActionResult> OnPostSaveRowsAsync(int? saveSingleRowId)
    {
        var settings = await _db.HallSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();

        var areaName = settings?.StationAreaName?.Trim();
        if (string.IsNullOrWhiteSpace(areaName))
            areaName = "Hotovo";

        var maxPositions = await GetMaxStationsForAreaAsync(areaName);

        var query = _db.HallRows.Include(r => r.Slots).AsQueryable();
        if (saveSingleRowId.HasValue)
            query = query.Where(r => r.Id == saveSingleRowId.Value);

        var rows = await query.ToListAsync();

        var totalCurrentCapacity = await _db.HallRows.SumAsync(r => r.Capacity);
        var totalDelta = 0;

        foreach (var row in rows)
        {
            if (RowCapacity.TryGetValue(row.Id, out var newCap))
            {
                if (newCap < 0) newCap = 0;
                totalDelta += (newCap - row.Capacity);
            }
        }

        if (maxPositions.HasValue && (totalCurrentCapacity + totalDelta) > maxPositions.Value)
        {
            ErrorMessage =
                $"Nelze uložit zmìny – oblast '{areaName}' má v Agiloxu max {maxPositions.Value} pozic.";
            return RedirectToPage();
        }

        foreach (var row in rows)
        {
            if (RowName.TryGetValue(row.Id, out var newName) && !string.IsNullOrWhiteSpace(newName))
                row.Name = newName.Trim();

            if (RowColor.TryGetValue(row.Id, out var newColor) && !string.IsNullOrWhiteSpace(newColor))
                row.ColorHex = newColor.Trim();

            if (RowCapacity.TryGetValue(row.Id, out var newCap))
            {
                if (newCap < 0) newCap = 0;

                if (newCap != row.Capacity)
                {
                    var ok = await ApplyCapacityChangeAsync(row, newCap);
                    if (!ok)
                        return RedirectToPage();
                }
            }
        }

        await _db.SaveChangesAsync();

        SuccessMessage = saveSingleRowId.HasValue
            ? "Øada byla uložena."
            : "Øady byly uloženy.";

        return RedirectToPage();
    }


    /// <summary>
    /// Aplikuje zmìnu kapacity øady:
    /// - pøi snížení nesmí "odøíznout" obsazené sloty
    /// - pøi zvýšení doplní chybìjící sloty
    /// </summary>
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
