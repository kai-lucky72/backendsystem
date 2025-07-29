using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Group;

public class GroupPerformanceDTO
{
    // Group identification
    public long GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    
    // Team leader information
    public long? TeamLeaderId { get; set; }
    public string? TeamLeaderWorkId { get; set; }
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
    public double? AverageClientsCollected { get; set; }
    public double? AverageClientsPerDay { get; set; }
    public long? TotalClientsCollected { get; set; }
    
    // Trend metrics
    public double? ChangeFromPreviousPeriod { get; set; }
    public string PerformanceTrend { get; set; } = string.Empty;  // e.g., "IMPROVING", "STEADY", "DECLINING"
    
    // Comparative metrics
    public int? RankAmongGroups { get; set; }
    public double? PerformanceRelativeToAverage { get; set; }
    
    public class GroupMemberSummaryDTO
    {
        public long AgentId { get; set; }
        public string WorkId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public double? AttendancePercentage { get; set; }
        public long? ClientsCollected { get; set; }
        public double? PerformanceScore { get; set; }
    }
}