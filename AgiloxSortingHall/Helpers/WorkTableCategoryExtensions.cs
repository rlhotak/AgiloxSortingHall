using AgiloxSortingHall.Enums;

namespace AgiloxSortingHall.Helpers
{
    public static class WorkTableCategoryExtensions
    {
        public static string ToUiText(this WorkTableCategory c) => c switch
        {
            WorkTableCategory.Kontrola => "Kontrola",
            WorkTableCategory.Pracoviste1 => "Pracoviště 1",
            WorkTableCategory.Pracoviste2 => "Pracoviště 2",
            WorkTableCategory.Pracoviste3 => "Pracoviště 3",
            WorkTableCategory.Pracoviste4 => "Pracoviště 4",
            WorkTableCategory.Pracoviste5 => "Pracoviště 5",
            WorkTableCategory.Pracoviste6 => "Pracoviště 6",
            WorkTableCategory.Pracoviste7 => "Pracoviště 7",
            WorkTableCategory.Pracoviste8 => "Pracoviště 8",
            WorkTableCategory.Pracoviste9 => "Pracoviště 9",
            _ => c.ToString()
        };
    }
}
