namespace RestaurantManager.ViewModels
{
    public class MonthEntryViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName =>
            System.Globalization.CultureInfo.InvariantCulture
                 .DateTimeFormat.GetMonthName(Month);
    }
}
