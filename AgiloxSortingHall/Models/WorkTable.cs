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
        /// Zobrazovaný název stolu (např. "Stůl 4").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kategorie stolu (např. Kontrola, Pracoviště 7)
        /// </summary>
        public WorkTableCategory Category { get; set; } = WorkTableCategory.Unknown;
    }

}
