namespace AgiloxSortingHall.Dto
{
    using System.Text.Json.Serialization;

    public class StationDto
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("stationarea")]
        public List<string> StationArea { get; set; } = new();
    }
}
