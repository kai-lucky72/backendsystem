using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Admin;

public class AdminDashboardDTO
{
    public List<SystemMetric> SystemMetrics { get; set; } = new();
    public AdminDashboardDTO.UserActivity UserActivity { get; set; } = new();
    public List<RecentSystemActivity> RecentSystemActivities { get; set; } = new();
    
    public class SystemMetric
    {
        public string Name { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Activity { get; set; }
    }
   
    public class UserActivity
    {
        public CountWithChange Managers { get; set; } = new();
        public CountWithChange Agents { get; set; } = new();
        public CountWithChange ActiveToday { get; set; } = new();
        public CountWithChange NotificationsSent { get; set; } = new();
    }
    
    public class CountWithChange
    {
        public int Count { get; set; }
        public string Change { get; set; } = string.Empty;
    }
    
    public class RecentSystemActivity
    {
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }
}