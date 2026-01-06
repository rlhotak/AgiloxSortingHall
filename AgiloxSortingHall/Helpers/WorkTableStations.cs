using AgiloxSortingHall.Enums;
using AgiloxSortingHall.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AgiloxSortingHall.Helpers
{
    public static class WorkTableStations
    {
        public static string GetInputStation(WorkTable t)
            => t.InputStationName;

        public static string GetOutputStation(WorkTable t)
            => t.OutputStationName;

        public static string GetUiName(WorkTable t)
            => t.DisplayName;

        public static string GetDestination(WorkTable t)
            => t.Category != WorkTableCategory.Kontrola ? "Kontrola" : "Hotovo";

        public static bool MatchesStation(WorkTable t, string? station)
        {
            if (string.IsNullOrWhiteSpace(station)) return false;

            return string.Equals(t.InputStationName, station, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.OutputStationName, station, StringComparison.OrdinalIgnoreCase);
        }

        public static string StationsToString(WorkTable t)
            => $"in={t.InputStationName}, out={t.OutputStationName}";
    }
}
