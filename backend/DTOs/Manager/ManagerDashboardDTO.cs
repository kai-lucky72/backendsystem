using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Manager;

public class ManagerDashboardDTO
{
    public ManagerDashboardDTO.Stats Stats { get; set; } = new();
    public ManagerDashboardDTO.Attendance Attendance { get; set; } = new();
    public List<GroupPerformanceItem> GroupPerformance { get; set; } = new();
    public List<IndividualPerformanceItem> IndividualPerformance { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();

    public class Stats
    {
        public int TotalAgents { get; set; }
        public int ActiveToday { get; set; }
        public int ClientsCollected { get; set; }
        public int GroupsCount { get; set; }
    }
    public class Attendance
    {
        public int Rate { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public List<PresentAgent> PresentAgents { get; set; } = new();
        public Timeframe Timeframe { get; set; } = new();
    }

    public class PresentAgent
    {
        public string Name { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class Timeframe
    {
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class GroupPerformanceItem
    {
        public string Name { get; set; } = string.Empty;
        public int Clients { get; set; }
    }

    public class IndividualPerformanceItem
    {
        public string Name { get; set; } = string.Empty;
        public int Clients { get; set; }
    }

    public class RecentActivity
    {
        public string Description { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}