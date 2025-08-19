using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Dashboard;

public class DashboardMetricsDTO
{
    // Basic stats
    public long? TotalAgents { get; set; }
    public long? ActiveAgents { get; set; }
    public long? InactiveAgents { get; set; }
    public long? TotalManagers { get; set; }
    public long? TotalGroups { get; set; }
    
    // Time period
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    
    // Attendance metrics
    public double? OverallAttendanceRate { get; set; }
    public int? TotalAttendanceRecords { get; set; }
    public int? AgentsWithPerfectAttendance { get; set; }
    public int? AgentsWithLowAttendance { get; set; }
    
    // Performance metrics (clients removed)
    public long? TotalClientsCollected { get; set; } = 0;
    public double? AverageClientsPerAgent { get; set; } = 0;
    public double? AverageClientsPerActiveDay { get; set; } = 0;
    
    // Agent type breakdown
    public Dictionary<string, int> AgentsByType { get; set; } = new();
    public Dictionary<string, double> PerformanceByAgentType { get; set; } = new();
    
    // Group performance
    public List<GroupSummaryDTO> TopPerformingGroups { get; set; } = new();
    public List<GroupSummaryDTO> LowPerformingGroups { get; set; } = new();
    
    // Recent activity
    public List<RecentActivityDTO> RecentActivities { get; set; } = new();
    
    public class GroupSummaryDTO
    {
        public long? GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public int? MemberCount { get; set; }
        public double? AttendanceRate { get; set; }
        public long? TotalClients { get; set; } = 0;
        public double? PerformanceScore { get; set; }
    }
    
    public class RecentActivityDTO
    {
        public string ActivityType { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}