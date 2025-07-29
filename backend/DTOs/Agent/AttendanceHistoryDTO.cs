using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class AttendanceHistoryDTO
{
    public int AttendanceRate { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public List<AttendanceRecord> Records { get; set; } = new();

    public class AttendanceRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}