namespace langla_duky.Models
{
    public class PerformanceStats
    {
        public double SuccessRate { get; set; }
        public MonitoringStats? MonitoringStats { get; set; }
    }

    public class MonitoringStats
    {
        public int TotalChecks { get; set; }
        public double DetectionRate { get; set; }
        public int CurrentInterval { get; set; }
    }
}
