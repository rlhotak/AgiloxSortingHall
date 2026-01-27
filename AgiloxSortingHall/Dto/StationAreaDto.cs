using System.Text.Json.Serialization;

namespace AgiloxSortingHall.Dto
{
    /// <summary>
    /// DTO objekt reprezentující stav oblasti (stationarea)
    /// vrácený REST API systému Agilox.
    /// </summary>
    public class StationAreaDto
    {
        /// <summary>
        /// Indikuje, zda je oblast přetížená (late occupation).
        /// </summary>
        [JsonPropertyName("occupation_late")]
        public bool OccupationLate { get; set; }

        /// <summary>
        /// Indikuje, zda je povoleno rychlé opuštění oblasti.
        /// </summary>
        [JsonPropertyName("fast_leave")]
        public bool FastLeave { get; set; }

        /// <summary>
        /// Celkový počet pozic (stanic) v dané oblasti podle Agiloxu.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// Počet blokovaných / dočasně nepoužitelných pozic v oblasti.
        /// </summary>
        [JsonPropertyName("count_blocked")]
        public int CountBlocked { get; set; }

        /// <summary>
        /// Určuje, zda se jedná o dopravníkovou oblast.
        /// </summary>
        [JsonPropertyName("conveyor")]
        public bool Conveyor { get; set; }
    }
}
