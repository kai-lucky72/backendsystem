using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class AgentPerformanceDTO
{
    public AgentInfo Agent { get; set; } = new();
    public PerformanceStats Stats { get; set; } = new();
    public List<ChartDataPoint> ChartData { get; set; } = new();
    public TrendData Trends { get; set; } = new();
    public string Period { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    
    public class AgentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
    
    public class PerformanceStats
    {
        public int TotalAttendanceDays { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public long TotalClients { get; set; } = 0;
        public double AttendanceRate { get; set; }
        public double ClientCollectionRate { get; set; } = 0;
    }
    
    public class ChartDataPoint
    {
        public string Date { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public int Clients { get; set; }
        public bool Attendance { get; set; }
    }
    
    public class TrendData
    {
        public List<PerformanceTrend> Trends { get; set; } = new();
        public double WeeklyGrowth { get; set; }
        public double MonthlyGrowth { get; set; }
    }
    
    public class PerformanceTrend
    {
        public string Name { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public long TotalClients { get; set; } = 0;
        public double AttendanceRate { get; set; }
        public double ClientCollectionRate { get; set; } = 0;
    }
}