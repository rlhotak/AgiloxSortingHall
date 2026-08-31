using AgiloxSortingHall.Data;
using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgiloxSortingHall.Services
{
    /// <summary>
    /// Konfigurace jedné řady na hale.
    /// </summary>
    public record HallRowConfig(string Name, string ColorHex, int Capacity);

    /// <summary>
    /// Konfigurace jednoho pracovního stolu (logický stůl viditelný v UI),
    /// včetně Agilox vstupní/výstupní stanice a kategorie pro filtr.
    /// </summary>
    public record WorkTableConfig(
        string DisplayName,
        string InputStationName,
        string OutputStationName,
        int Category
    );

    /// <summary>
    /// Konfigurace haly (řady + stoly).
    /// </summary>
    public class HallConfig
    {
        public List<HallRowConfig> Rows { get; set; } = new();
        public List<WorkTableConfig> Tables { get; set; } = new();
    }

    /// <summary>
    /// Třída umožňující inicializaci databáze podle konfigurace haly (řady a stoly).
    /// </summary>
    public class DataSeeder
    {
        private readonly AppDbContext _db;
        private readonly HallConfig _config;

        /// <summary>
        /// Inicializuje DataSeeder injektovaným AppDbContextem a konfiguračními daty haly.
        /// </summary>
        public DataSeeder(AppDbContext db, IOptions<HallConfig> config)
        {
            _db = db;
            _config = config.Value;
        }

        /// <summary>
        /// Naplní databázi výchozími daty:
        /// - vytvoří/aktualizuje řady dle konfigurace, včetně slotů
        /// - vytvoří/aktualizuje pracovní stoly dle konfigurace
        /// Metoda je idempotentní (opakované spuštění nic nezdvojí).
        /// </summary>
        public async Task SeedAsync()
        {
            // Řady (HallRows)
            foreach (var rowCfg in _config.Rows)
            {
                var row = await _db.HallRows
                    .Include(r => r.Slots)
                    .FirstOrDefaultAsync(r => r.Name == rowCfg.Name);

                if (row == null)
                {
                    row = new HallRow
                    {
                        Name = rowCfg.Name,
                        ColorHex = rowCfg.ColorHex,
                        Capacity = rowCfg.Capacity
                    };

                    for (int i = 0; i < rowCfg.Capacity; i++)
                    {
                        row.Slots.Add(new PalletSlot
                        {
                            PositionIndex = i,
                            State = PalletState.Empty
                        });
                    }

                    _db.HallRows.Add(row);
                }
                else
                {
                    row.ColorHex = rowCfg.ColorHex;
                    row.Capacity = rowCfg.Capacity;

                    // Odstranit sloty navíc
                    var extraSlots = row.Slots
                        .Where(s => s.PositionIndex >= rowCfg.Capacity)
                        .ToList();

                    if (extraSlots.Any())
                        _db.PalletSlots.RemoveRange(extraSlots);

                    // Přidat chybějící sloty
                    for (int i = 0; i < rowCfg.Capacity; i++)
                    {
                        if (!row.Slots.Any(s => s.PositionIndex == i))
                        {
                            row.Slots.Add(new PalletSlot
                            {
                                PositionIndex = i,
                                State = PalletState.Empty
                            });
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            // Stoly (WorkTables) – logické stoly pro UI, s Agilox vstup/výstup stanicí
            foreach (var tblCfg in _config.Tables)
            {
                // Unikátní klíč pro idempotenci: podle vstupní stanice (musí být unikátní)
                var table = await _db.WorkTables
                    .FirstOrDefaultAsync(t => t.InputStationName == tblCfg.InputStationName);

                if (table == null)
                {
                    _db.WorkTables.Add(new WorkTable
                    {
                        DisplayName = tblCfg.DisplayName,
                        InputStationName = tblCfg.InputStationName,
                        OutputStationName = tblCfg.OutputStationName,
                        Category = (WorkTableCategory)tblCfg.Category
                    });
                }
                else
                {
                    // aktualizace existujícího záznamu (sync s configem)
                    table.DisplayName = tblCfg.DisplayName;
                    table.InputStationName = tblCfg.InputStationName;
                    table.OutputStationName = tblCfg.OutputStationName;
                    table.Category = (WorkTableCategory)tblCfg.Category;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
