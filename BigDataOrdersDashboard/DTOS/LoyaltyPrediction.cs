using Microsoft.ML.Data;

namespace BigDataOrdersDashboard.DTOS
{
    public class LoyaltyPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }
    }
}
