namespace BigDataOrdersDashboard.Models
{
    public class NumericStatisticsModel
    {
        public int TotalOrder {  get; set; }
        public int TotalProductCount { get; set; }
        public int TotalCustomerCount { get; set; }
        public int TotalCategoryCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public int ThisMonthOrderCount { get; set; }
        public int CompletedOrderCount { get; set; }
        public int PendingOrderCount { get; set; }
        public int ShippedOrderCount { get; set; }
        public int CancelledOrderCount { get; set; }
        public string MaxPriceProductName { get; set; }
        public decimal MaxProductPrice { get; set; }
        public string MinPriceProductName { get; set; }
        public decimal MinProductPrice { get; set; }
        public decimal AverageProductPrice { get; set; }
        public int TotalQuantityCount { get; set; }
        public int CriticalStockProductCount { get; set; }
        public int DistinctCountryCount {  get; set; }
        public int InactiveCustomerCount { get; set; }
        public decimal TotalStockValue { get; set; }
        public decimal TotalRevenue {  get; set; }
        public decimal AverageBasket { get; set; }
        public string TopCustomerName { get; set; }
        public int TopCustomerOrderCount { get; set; }

    }
}
