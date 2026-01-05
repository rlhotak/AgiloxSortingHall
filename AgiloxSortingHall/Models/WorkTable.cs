using AgiloxSortingHall.Enums;

namespace AgiloxSortingHall.Models
{
    /// <summary>
    /// Pracovní stůl v hale, ze kterého mohou být odesílány požadavky
    /// na paletu z konkrétní řady.
    /// </summary>
    public class WorkTable
    {
        /// <summary>
        /// Primární klíč pracovního stolu.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Zobrazovaný název stolu (např. "Stůl 1").
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Název stanice v Agilox systému pro vstup.
        /// </summary>
        public string InputStationName { get; set; } = null!;

        /// <summary>
        /// Název stanice v Agilox systému pro výstup.
        /// </summary>
        public string OutputStationName { get; set; } = null!;

        /// <summary>
        /// Kategorie stolu (např. Kontrola, Pracoviště 7)
        /// </summary>
        public WorkTableCategory Category { get; set; } = WorkTableCategory.Unknown;
    }

}