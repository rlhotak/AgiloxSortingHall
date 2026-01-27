using AgiloxSortingHall.Enums;

namespace AgiloxSortingHall.Models
{
    /// <summary>
    /// Globální nastavení haly.
    /// Ovládá ji skladník.
    /// </summary>
    public class HallSettings
    {
        /// <summary>
        /// Primární klíč (typicky 1).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Zvolená strategie pro výběr řady při volání artiklu ze stolu.
        /// </summary>
        public RowSelectionStrategy RowSelectionStrategy { get; set; }
            = RowSelectionStrategy.MostFreePallets;

        /// <summary>
        /// Název oblasti (stationarea) v systému Agilox, která určuje:
        /// - maximální počet reálných pozic/stanic v Agiloxu (limit přidání řad)
        /// - "kontext" pro skladníka (na jakou oblast se nastavení vztahuje)
        ///
        /// Default: "Hotovo" (dřívější fixní stav).
        /// </summary>
        public string StationAreaName { get; set; } = "Hotovo";
    }
}
