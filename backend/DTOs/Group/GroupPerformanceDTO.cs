using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Group;

public class GroupPerformanceDTO
{
    // Group identification
    public long GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Team leader information
    public long? TeamLeaderId { get; set; }
    // WorkId deprecated
    public string? TeamLeaderPhone { get; set; }
    public string? TeamLeaderEmail { get; set; }
    
    // Group composition
    public int? MemberCount { get; set; }
    public List<GroupMemberSummaryDTO> TopPerformers { get; set; } = new();
    
    // Time period
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? TotalDaysInPeriod { get; set; }
    
    // Aggregated performance metrics
    public double? AverageAttendancePercentage { get; set; }
    public double? AverageDaysPresent { get; set; }
    public double? AverageClientsCollected { get; set; } = 0;
    public double? AverageClientsPerDay { get; set; } = 0;
    public long? TotalClientsCollected { get; set; } = 0;
    
    // Trend metrics
    public double? ChangeFromPreviousPeriod { get; set; }
    public string PerformanceTrend { get; set; } = string.Empty;  // e.g., "IMPROVING", "STEADY", "DECLINING"
    
    // Comparative metrics
    public int? RankAmongGroups { get; set; }
    public double? PerformanceRelativeToAverage { get; set; }
    
    public class GroupMemberSummaryDTO
    {
        public long AgentId { get; set; }
        public string Email { get; set; } = string.Empty;
        public double? AttendancePercentage { get; set; }
        public long? ClientsCollected { get; set; } = 0;
        public double? PerformanceScore { get; set; }
    }
}