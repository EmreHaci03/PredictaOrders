namespace BigDataOrdersDashboard.DTOS
{
    public class LoyaltyScoreDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpend { get; set; }
        public DateTime LastOrderDate { get; set; }
        public int DaysSinceLastOrder { get; set; }
        public int LoyaltyScore { get; set; }
        public string LoyaltyLevel { get; set; } = string.Empty;
    }
}
