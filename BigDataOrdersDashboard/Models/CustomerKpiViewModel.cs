namespace BigDataOrdersDashboard.Models
{
    public class CustomerKpiViewModel
    {
        public int TotalOrderCount { get; set; }
        public int UniqueProductCount { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal AverageBasketAmount { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public int? DaysSinceLastOrder { get; set; }
    }
}
