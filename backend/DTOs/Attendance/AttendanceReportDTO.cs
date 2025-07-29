using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Attendance;

public class AttendanceReportDTO
{
    // Report metadata
    public string ReportType { get; set; } = string.Empty;  // Daily, Weekly, Monthly, Custom
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? TotalWorkingDays { get; set; }
    
    // Overall statistics
    public double? OverallAttendanceRate { get; set; }
    public double? AverageArrivalTime { get; set; }
    public int? TotalRecords { get; set; }
    public int? TotalAgents { get; set; }
    
    // Agent attendance summary
    public List<AgentAttendanceSummaryDTO> AgentSummaries { get; set; } = new();
    
    // Trends and patterns
    public Dictionary<string, double> AttendanceByDay { get; set; } = new();  // Day of week -> attendance rate
    public Dictionary<DateOnly, double> AttendanceTrend { get; set; } = new();  // Date -> attendance rate
    public Dictionary<int, double> AttendanceByHour { get; set; } = new();  // Hour of day (arrival) -> attendance count
    
    // Punctuality metrics
    public int? EarlyArrivals { get; set; }
    public int? OnTimeArrivals { get; set; }
    public int? LateArrivals { get; set; }
    public double? AverageLateMinutes { get; set; }
    
    // Comparison to previous period
    public double? PreviousPeriodAttendanceRate { get; set; }
    public double? AttendanceRateChange { get; set; }  // Positive or negative change
    public string TrendDirection { get; set; } = string.Empty;  // IMPROVING, STEADY, DECLINING
    
    public class AgentAttendanceSummaryDTO
    {
        public long AgentId { get; set; }
        public string WorkId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        
        // Attendance statistics
        public int? DaysPresent { get; set; }
        public int? DaysAbsent { get; set; }
        public double? AttendanceRate { get; set; }
        public double? PunctualityRate { get; set; }  // Percentage of on-time arrivals
        
        // Arrival patterns
        public TimeOnly? AverageArrivalTime { get; set; }
        public TimeOnly? EarliestArrival { get; set; }
        public TimeOnly? LatestArrival { get; set; }
        public int? LateArrivalCount { get; set; }
        public int? EarlyArrivalCount { get; set; }
        
        // Trends
        public string AttendanceTrend { get; set; } = string.Empty;  // IMPROVING, CONSISTENT, DECLINING
    }
}