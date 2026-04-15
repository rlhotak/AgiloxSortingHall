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
        /// Zvolená strategie pro výběr řady při pokládání hotových palet.
        /// </summary>
        public DropRowSelectionStrategy DropRowSelectionStrategy { get; set; }
            = DropRowSelectionStrategy.NearestRight;


        /// <summary>
        /// Název oblasti (stationarea) v systému Agilox, díky které můžeme určit:
        /// - maximální počet reálných pozic/stanic v Agiloxu (limit přidání řad)
        /// - "kontext" pro skladníka (na jakou oblast se nastavení vztahuje)
        ///
        /// Default: "Buffer"
        /// </summary>
        public string PickupStationAreaName { get; set; } = "Buffer";

        /// <summary>
        /// Název oblasti (stationarea) v systému Agilox, díky které můžeme určit:
        /// - kam může Agilox posílat palety od stanice kontroly
        ///
        /// Default: "Hotovo"
        /// </summary>
        public string DropStationAreaName { get; set; } = "Hotovo";
    }
}
