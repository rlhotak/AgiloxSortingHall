using AgiloxSortingHall.Data;
using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Helpers;
using AgiloxSortingHall.Models;
using AgiloxSortingHall.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AgiloxSortingHall.Pages
{
    /// <summary>
    /// Úvodní stránka – přehled všech stolů a jejich aktuálního stavu.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(
            AppDbContext db,
            ILogger<IndexModel> logger,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Přehledové položky pro jednotlivé stoly
        /// (stůl + pending call + poslední call).
        /// </summary>
        public List<TableOverviewViewModel> Tables { get; set; } = new();

        /// <summary>
        /// Aktuálně zvolená kategorie z query stringu, např. ?category=Kontrola.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public WorkTableCategory? Category { get; set; }

        /// <summary>
        /// Možnosti do dropdownu filtru kategorií (bez Unknown).
        /// </summary>
        public IEnumerable<WorkTableCategory> CategoryOptions { get; } =
            Enum.GetValues<WorkTableCategory>()
                .Where(x => x != WorkTableCategory.Unknown);

        public async Task OnGetAsync()
        {
            var tablesQuery = _db.WorkTables.AsQueryable();

            if (Category.HasValue)
                tablesQuery = tablesQuery.Where(t => t.Category == Category.Value);

            // Nemáš .Name → řadíme podle friendly názvu
            var tables = await tablesQuery
                .OrderBy(t => t.DisplayName)
                .ToListAsync();

            var tableIds = tables.Select(t => t.Id).ToList();
            if (!tableIds.Any())
            {
                Tables = new();
                return;
            }

            var pendingCalls = await _db.RowCalls
                .Include(c => c.HallRow)
                .Where(c =>
                    tableIds.Contains(c.WorkTableId) &&
                    c.Status == RowCallStatus.Pending)
                .ToListAsync();

            var pendingByTable = pendingCalls
                .GroupBy(c => c.WorkTableId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.RequestedAt).First()
                );

            var lastCalls = await _db.RowCalls
                .Include(c => c.HallRow)
                .Where(c => tableIds.Contains(c.WorkTableId))
                .GroupBy(c => c.WorkTableId)
                .Select(g => g
                    .OrderByDescending(c => c.RequestedAt)
                    .First())
                .ToListAsync();

            var lastByTable = lastCalls
                .ToDictionary(c => c.WorkTableId, c => c);

            Tables = tables
                .Select(t =>
                {
                    pendingByTable.TryGetValue(t.Id, out var pending);
                    lastByTable.TryGetValue(t.Id, out var last);

                    return new TableOverviewViewModel
                    {
                        Table = t,
                        PendingCall = pending,
                        LastCall = last
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Textový popis aktivity pro daný RowCall (kvůli kompatibilitě).
        /// </summary>
        public string GetActivityDescription(RowCall call)
            => AgiloxActivityDescriptionHelper.GetActivityDescription(call);

        /// <summary>
        /// Handler pro tlačítko "Odvézt" na indexu.
        /// Pošle na Agilox workflow 502 s OUTPUT stanicí stolu.
        /// </summary>
        public async Task<IActionResult> OnPostDoneAsync(int tableId)
        {
            var table = await _db.WorkTables.FindAsync(tableId);
            if (table == null)
            {
                _logger.LogWarning("OnPostDoneAsync: stůl {TableId} nebyl nalezen.", tableId);
                return RedirectToPage();
            }

            // vytvoříme RowCall bez řady – reprezentuje "odvoz od stolu"
            var call = new RowCall
            {
                WorkTableId = table.Id,
                HallRowId = null,
                Status = RowCallStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _db.RowCalls.Add(call);
            await _db.SaveChangesAsync();

            var client = _httpClientFactory.CreateClient("Agilox");

            // pro Agilox bereme OUTPUT station z helperu
            var station = WorkTableStations.GetOutputStation(table);

            var payload = new Dictionary<string, string>
            {
                ["@TABLE"] = station
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "OnPostDoneAsync: posílám workflow 502 pro stůl {Table}. Station={Station}. Payload={Payload}",
                table.DisplayName,
                station,
                json);

            var response = await client.PostAsync("workflow/502", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "OnPostDoneAsync: Agilox odpověď pro stůl {Table}: {Body}",
                table.DisplayName,
                responseBody);

            response.EnsureSuccessStatusCode();

            // zkus vytáhnout ID z odpovědi Agiloxu a uložit do RowCall.OrderId
            try
            {
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("id", out var idProp))
                {
                    long? agiloxId = null;

                    if (idProp.ValueKind == JsonValueKind.Number &&
                        idProp.TryGetInt64(out var numericId))
                    {
                        agiloxId = numericId;
                    }
                    else if (idProp.ValueKind == JsonValueKind.String &&
                             long.TryParse(idProp.GetString(), out var stringId))
                    {
                        agiloxId = stringId;
                    }

                    if (agiloxId.HasValue)
                    {
                        call.OrderId = agiloxId.Value;
                        await _db.SaveChangesAsync();

                        _logger.LogInformation(
                            "OnPostDoneAsync: RowCall {RowCallId} pro stůl {Table} má OrderId={OrderId}",
                            call.Id, table.DisplayName, call.OrderId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "OnPostDoneAsync: odpověď Agiloxu neobsahuje použitelné 'id'. Body={Body}",
                            responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OnPostDoneAsync: chyba při parsování odpovědi Agiloxu: {Body}",
                    responseBody);
            }

            return RedirectToPage(new { category = Category });
        }
    }
}
