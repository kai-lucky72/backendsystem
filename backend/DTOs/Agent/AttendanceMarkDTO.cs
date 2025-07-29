using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Agent;

public class AttendanceMarkDTO
{
    public string Message { get; set; } = string.Empty;
    public AttendanceInfo Attendance { get; set; } = new();

    public class AttendanceInfo
    {
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}