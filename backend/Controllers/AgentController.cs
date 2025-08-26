 
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;

namespace backend.Controllers;

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}

[ApiController]
[Route("api/agent")]
[Authorize(Roles = "admin,manager,agent")]
public class AgentController(
        IAgentService agentService,
        IAttendanceService attendanceService,
        
        IAttendanceTimeframeService attendanceTimeframeService,
        IGroupService groupService,
        INotificationService notificationService,
        IExternalClientService externalClientService)
    : ControllerBase
{
    // External-only mode: sync endpoint is a no-op or removed
    [HttpPost("clients/sync")]
    public Task<ActionResult> SyncMyClients(CancellationToken ct) => Task.FromResult<ActionResult>(Ok(new { status = "noop" }));

    [HttpGet("clients")]
    public async Task<ActionResult<object>> GetMyClients([FromQuery] string? from = null, [FromQuery] string? to = null, [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId)) return Unauthorized();

        var agent = await agentService.GetAgentByIdAsync(userId);
        if (agent == null) return NotFound(new { error = "Agent not found" });
        if (string.IsNullOrWhiteSpace(agent.ExternalDistributionChannelId))
            return Conflict(new { error = "Agent is not linked to external distribution channel" });

        DateTime fromDate;
        if (string.Equals(from, "auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(from))
            fromDate = agent.User.CreatedAt.Date;
        else if (!DateTime.TryParse(from, out fromDate))
            return BadRequest(new { error = "Invalid from date" });

        DateTime? toDate = null;
        if (!string.IsNullOrWhiteSpace(to))
        {
            if (DateTime.TryParse(to, out var td)) toDate = td; else return BadRequest(new { error = "Invalid to date" });
        }

        var external = await externalClientService.GetProposalsByDistributionChannelAsync(agent.ExternalDistributionChannelId!, ct);
        var filtered = external
            .Where(p => p.ProposalDate.HasValue)
            .Where(p => p.ProposalDate!.Value.Date >= fromDate.Date && (!toDate.HasValue || p.ProposalDate!.Value.Date <= toDate.Value.Date))
            .OrderByDescending(p => p.ProposalDate)
            .ToList();

        var total = filtered.Count;
        if (page < 1) page = 1;
        if (limit < 1) limit = 20;
        var items = filtered
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new {
                proposalNumber = p.ProposalNumber,
                customerName = p.CustomerName,
                proposalDate = p.ProposalDate,
                premium = p.TotalPremium
            })
            .ToList();

        return Ok(new { items, page, limit, total });
    }
    [HttpPost("attendance")]
    public async Task<ActionResult> MarkAttendance([FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        var location = body.GetValueOrDefault("location", "");
        var sector = body.GetValueOrDefault("sector", "");
        var attendance = await attendanceService.MarkAttendanceAsync(agent, location, sector);
        var time = attendance.Timestamp?.ToString("HH:mm") ?? "--:--";
        var date = attendance.Timestamp?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
        
        // Calculate status based on timeframe and 5-minute rule
        var status = await CalculateAttendanceStatusAsync(agent, attendance.Timestamp);
        
        var attendanceInfo = new Dictionary<string, object>
        {
            ["date"] = date,
            ["time"] = time,
            ["status"] = status
        };
        var response = new Dictionary<string, object>
        {
            ["message"] = $"Attendance marked successfully at {time}",
            ["attendance"] = attendanceInfo
        };
        return StatusCode(201, response);
    }

    [HttpGet("attendance/history")]
    public async Task<ActionResult> GetAttendanceHistory()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        var all = await attendanceService.GetAttendanceByAgentAsync(agent);
    
        int present = 0, late = 0 ;
        var records = new List<Dictionary<string, object>>();
    
        foreach (var att in all)
        {
            var status = await CalculateAttendanceStatusAsync(agent, att.Timestamp);
            if (status == "present") present++; else late++;
        
            records.Add(new Dictionary<string, object>
            {
                ["id"] = $"rec-{att.Id}",
                ["date"] = att.Timestamp?.ToString("yyyy-MM-dd") ?? "--/--/----",
                ["time"] = att.Timestamp?.ToString("HH:mm") ?? "--:--",
                ["location"] = att.Location,
                ["sector"] = att.Sector,
                ["status"] = status
            });
        }
    
        // FIXED: Match Java's attendance calculation logic
var attendanceDates = all
    .Where(a => a.Timestamp.HasValue)
    .Select(a => a.Timestamp.Value.Date)
    .Distinct()
    .ToList();
    
        int totalDays = attendanceDates.Count;

        // FIXED: Calculate absent count properly (Java doesn't seem to calculate this correctly either)
        // For now, keep absent = 0 to match Java behavior
        var absent = 0;

        // FIXED: Use present count vs. totalDays like Java
        int attendanceRate = totalDays > 0 ? (int)((present / (double)totalDays) * 100) : 0;
    
        var response = new Dictionary<string, object>
        {
            ["attendanceRate"] = attendanceRate,
            ["presentCount"] = present,
            ["lateCount"] = late,
            ["absentCount"] = absent, // This matches Java's logic
            ["records"] = records
        };
    
        return Ok(response);
    }

    [HttpGet("attendance/timeframe")]
    public async Task<ActionResult> GetAttendanceTimeframe()
    {
        var timeframe = await attendanceTimeframeService.GetLatestTimeframeAsync();
        string start = timeframe?.StartTime.ToString() ?? "09:00";
        string end = timeframe?.EndTime.ToString() ?? "10:00";
        return Ok(new Dictionary<string, string> { ["startTime"] = start, ["endTime"] = end });
    }

    [HttpGet("attendance/status")]
    public async Task<ActionResult> GetAttendanceStatus()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        if (agent == null)
            return NotFound(new { error = $"Agent not found for user {userId}" });
        bool hasMarkedToday = await attendanceService.HasMarkedAttendanceTodayAsync(agent);
        var lastAttendance = await attendanceService.GetLastAttendanceTimeAsync(agent);
        return Ok(new Dictionary<string, object>
        {
            ["hasMarkedToday"] = hasMarkedToday,
            ["lastAttendanceTime"] = lastAttendance?.ToString() ?? ""
        });
    }

    // Clients collected removed

    // Clients collected removed

    // --- FULL DTO LOGIC FOR /performance ---
    [HttpGet("performance")]
    public async Task<ActionResult> GetPerformance([FromQuery] string period = "weekly", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        DateTime start, end;
        if (startDate.HasValue && endDate.HasValue)
        {
            start = startDate.Value;
            end = endDate.Value;
        }
        else
        {
            end = DateTime.Now;
            start = period.ToLower() switch
            {
                "weekly" => end.AddDays(-7),
                "monthly" => end.AddMonths(-1),
                _ => end.AddDays(-7)
            };
        }
        var attendances = (await attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, start, end)).ToList();
        var chartData = await GenerateChartData(agent, start, end, attendances);
        var stats = CalculatePerformanceStats(agent, start, end, attendances, 0, chartData);
        var trends = await CalculateTrends(agent, period, chartData);
        var agentInfo = new DTOs.Agent.AgentPerformanceDTO.AgentInfo
        {
            Id = $"agt-{agent.UserId:D3}",
            FirstName = agent.User.FirstName,
            LastName = agent.User.LastName,
            Email = agent.User.Email,
            Type = agent.AgentType.ToString().ToLower(),
            Status = agent.User.Active ? "active" : "inactive"
        };
        var dto = new DTOs.Agent.AgentPerformanceDTO
        {
            Agent = agentInfo,
            Stats = stats,
            ChartData = chartData,
            Trends = trends,
            Period = period,
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd")
        };
        return Ok(dto);
    }

    // --- FULL DTO LOGIC FOR /dashboard ---
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        bool attendanceMarked = await attendanceService.HasMarkedAttendanceTodayAsync(agent);
        var today = DateTime.Now;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
        // Clients collected via external fetch (filtered by local join date)
        var monthFrom = (agent.User.CreatedAt.Date > startOfMonth.Date) ? agent.User.CreatedAt.Date : startOfMonth.Date;
        var proposals = string.IsNullOrWhiteSpace(agent.ExternalDistributionChannelId)
            ? new List<backend.DTOs.External.ExternalProposalDto>()
            : (await externalClientService.GetProposalsByDistributionChannelAsync(agent.ExternalDistributionChannelId!)).ToList();
        var monthProposals = proposals.Where(p => p.ProposalDate.HasValue && p.ProposalDate.Value.Date >= monthFrom.Date && p.ProposalDate.Value.Date <= endOfMonth.Date).ToList();
        var totalProposals = proposals.Where(p => p.ProposalDate.HasValue && p.ProposalDate.Value.Date >= agent.User.CreatedAt.Date).ToList();
        double performanceRate = CalculatePerformanceRate(agent);
        var recentActivities = await GetRecentActivities(agent);
        var dto = new DTOs.Agent.AgentDashboardDTO
        {
            AttendanceMarked = attendanceMarked,
            ClientsThisMonth = monthProposals.Count,
            TotalClients = totalProposals.Count,
            PerformanceRate = performanceRate,
            RecentActivities = recentActivities
        };
        if (agent is { AgentType: AgentType.SALES, Group: not null })
        {
            dto.GroupName = agent.Group.Name;
            if (agent.Group.Leader != null)
            {
                var leaderUser = agent.Group.Leader.User;
                dto.TeamLeader = $"{leaderUser.FirstName} {leaderUser.LastName}";
            }
        }
        return Ok(dto);
    }

    // --- FULL DTO LOGIC FOR /group-performance ---
    [HttpGet("group-performance")]
    public async Task<ActionResult> GetGroupPerformance()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        if (agent.AgentType != AgentType.SALES || agent.Group == null)
            return StatusCode(403);
        var group = agent.Group;
        var dto = await BuildGroupPerformanceDto(group);
        return Ok(dto);
    }

    // --- FULL DTO LOGIC FOR /notifications ---
    [HttpGet("notifications")]
    public async Task<ActionResult> GetAgentNotifications()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);
        var notifications = await notificationService.GetNotificationsByRecipientAsync(agent.User);
    
        // FIXED: Match Java's response format exactly
        var notificationList = notifications.Select(n => {
            // Map priority to type like Java does
            string type = "info";
            if (n.Priority != null)
            {
                type = n.Priority.ToString().ToLower() switch
                {
                    "urgent" => "urgent",
                    "high" => "warning", 
                    "low" => "success",
                    _ => "info"
                };
            }
        
            // Create sender object like Java
            var senderObject = new Dictionary<string, object>();
            if (n.Sender != null)
            {
                senderObject["role"] = n.Sender.Role.ToString().ToLower();
                senderObject["name"] = $"{n.Sender.FirstName} {n.Sender.LastName}";
            }
        
            // Return a complete notification object matching Java format
            return new Dictionary<string, object>
            {
                ["id"] = $"notif-{n.Id}",
                ["title"] = n.Title ?? "", // ADDED: Missing title field
                ["message"] = n.Message ?? "",
                ["type"] = type, // FIXED: Proper type mapping
                ["sender"] = senderObject, // ADDED: Missing sender object
                ["timestamp"] = n.SentAt?.ToString() ?? throw new InvalidOperationException(message:"null value on timestamp in agentcontroller"),
                ["read"] = n.ReadStatus, // ADDED: Missing read status
                ["priority"] = n.Priority.ToString().ToLower() // ADDED: Missing priority
            };
        }).ToList();
    
        return Ok(new { notifications = notificationList });
    }

    // Client creation removed

    // Clients listing removed

    // Get client by id removed

    // Client search removed

    // Clients by date range removed

    // --- PRIVATE HELPERS ---
    private async Task<string> CalculateAttendanceStatusAsync(Agent agent, DateTime? timestamp)
    {
        if (!timestamp.HasValue)
            return "present";
            
        // Use the latest global timeframe
        var timeframe = await attendanceTimeframeService.GetLatestTimeframeAsync();
        
        var startTime = timeframe?.StartTime ?? new TimeOnly(6, 0); // Default 6:00 AM
        var endTime = timeframe?.EndTime ?? new TimeOnly(9, 0); // Default 9:00 AM
        
        var attendanceTime = TimeOnly.FromDateTime(timestamp.Value);
        
        // Check if attendance is within the timeframe
        if (attendanceTime < startTime || attendanceTime > endTime)
        {
            return "late"; // Outside timeframe is always late
        }
        
        // Check if there are 5 minutes or less remaining in the timeframe
        var timeRemaining = endTime - attendanceTime;
        if (timeRemaining.TotalMinutes <= 5)
        {
            return "late"; // 5 minutes or less remaining = late
        }
        
        return "present"; // Within timeframe with more than 5 minutes remaining
    }
    
    private async Task<List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint>> GenerateChartData(Agent agent, DateTime start, DateTime end, List<Attendance> attendances)
    {
        var chartData = new List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var startOfDay = date;
            var endOfDay = date.AddDays(1).AddSeconds(-1);
            var dayAttendances = attendances.Where(a => a.Timestamp.HasValue && a.Timestamp.Value.Date == date).ToList();
            bool hasAttendance = dayAttendances.Any();
            int present = 0, late = 0, absent = hasAttendance ? 0 : 1;
            foreach (var att in dayAttendances)
            {
                var status = await CalculateAttendanceStatusAsync(agent, att.Timestamp);
                if (status == "late")
                    late++;
                else
                    present++;
            }
            var clientCount = 0;
            chartData.Add(new DTOs.Agent.AgentPerformanceDTO.ChartDataPoint
            {
                Date = date.ToString("yyyy-MM-dd"),
                Present = present,
                Late = late,
                Absent = absent,
                Clients = clientCount,
                Attendance = hasAttendance
            });
        }
        return chartData;
    }

    private DTOs.Agent.AgentPerformanceDTO.PerformanceStats CalculatePerformanceStats(Agent agent, DateTime start, DateTime end, List<Attendance> attendances, long totalClients, List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint> chartData)
    {
        int presentCount = 0, lateCount = 0, absentCount = 0, totalDays = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var dayData = chartData.FirstOrDefault(c => c.Date == date.ToString("yyyy-MM-dd"));
            if (dayData != null)
            {
                presentCount += dayData.Present;
                lateCount += dayData.Late;
                absentCount += dayData.Absent;
                totalDays++;
            }
        }
        double attendanceRate = totalDays > 0 ? (double)presentCount / totalDays * 100 : 0;
        double clientCollectionRate = 0;
        return new DTOs.Agent.AgentPerformanceDTO.PerformanceStats
        {
            TotalAttendanceDays = totalDays,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            TotalClients = totalClients,
            AttendanceRate = attendanceRate,
            ClientCollectionRate = clientCollectionRate
        };
    }

    private async Task<DTOs.Agent.AgentPerformanceDTO.TrendData> CalculateTrends(Agent agent, string period, List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint> chartData)
    {
        var trends = new List<DTOs.Agent.AgentPerformanceDTO.PerformanceTrend>();
        var today = DateTime.Now.Date;
        DateTime startDate, endDate;
        switch (period.ToLower())
        {
            case "weekly":
                startDate = today.AddDays(-7);
                endDate = today;
                break;
            case "monthly":
                startDate = today.AddMonths(-1);
                endDate = today;
                break;
            default:
                startDate = today.AddDays(-7);
                endDate = today;
                break;
        }
        var attendances = (await attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, startDate, endDate)).ToList();
        var totalClients = 0L;
        var currentPeriodChartData = await GenerateChartData(agent, startDate, endDate, attendances);
        var currentPeriodStats = CalculatePerformanceStats(agent, startDate, endDate, attendances, totalClients, currentPeriodChartData);
        trends.Add(new DTOs.Agent.AgentPerformanceDTO.PerformanceTrend
        {
            Name = "Current Period",
            Present = currentPeriodStats.PresentCount,
            Late = currentPeriodStats.LateCount,
            Absent = currentPeriodStats.AbsentCount,
            TotalClients = 0,
            AttendanceRate = currentPeriodStats.AttendanceRate,
            ClientCollectionRate = currentPeriodStats.ClientCollectionRate
        });
        // Placeholder for previous period
        trends.Add(new DTOs.Agent.AgentPerformanceDTO.PerformanceTrend
        {
            Name = "Previous Period",
            Present = 0,
            Late = 0,
            Absent = 0,
            TotalClients = 0,
            AttendanceRate = 0,
            ClientCollectionRate = 0
        });
        return new DTOs.Agent.AgentPerformanceDTO.TrendData
        {
            Trends = trends,
            WeeklyGrowth = 0,
            MonthlyGrowth = 0
        };
    }

    private double CalculatePerformanceRate(Agent agent)
    {
        return 0;
    }

    private async Task<List<DTOs.Agent.AgentDashboardDTO.RecentActivity>> GetRecentActivities(Agent agent)
    {
        var items = new List<(DateTime when, DTOs.Agent.AgentDashboardDTO.RecentActivity activity)>();

        // Recent attendance events (last 5)
        var attendances = await attendanceService.GetAttendanceByAgentAsync(agent);
        foreach (var att in attendances
                     .Where(a => a.Timestamp.HasValue)
                     .OrderByDescending(a => a.Timestamp)
                     .Take(5))
        {
            var when = att.Timestamp!.Value;
            items.Add((when, new DTOs.Agent.AgentDashboardDTO.RecentActivity
            {
                Id = $"att-{att.Id}",
                Description = "Marked attendance",
                Timestamp = FormatTimeAgo(when)
            }));
        }

        // Recent collected proposals (last 5 since local join date)
        var proposals = string.IsNullOrWhiteSpace(agent.ExternalDistributionChannelId)
            ? new List<backend.DTOs.External.ExternalProposalDto>()
            : (await externalClientService.GetProposalsByDistributionChannelAsync(agent.ExternalDistributionChannelId!)).ToList();
        foreach (var p in proposals
                     .Where(p => p.ProposalDate.HasValue && p.ProposalDate.Value.Date >= agent.User.CreatedAt.Date)
                     .OrderByDescending(p => p.ProposalDate)
                     .Take(5))
        {
            var when = p.ProposalDate ?? DateTime.UtcNow;
            var customer = string.IsNullOrWhiteSpace(p.CustomerName) ? "Client" : p.CustomerName;
            var number = string.IsNullOrWhiteSpace(p.ProposalNumber) ? "proposal" : $"proposal {p.ProposalNumber}";
            items.Add((when, new DTOs.Agent.AgentDashboardDTO.RecentActivity
            {
                Id = $"prop-{number}",
                Description = $"Collected {number} for {customer}",
                Timestamp = FormatTimeAgo(when)
            }));
        }

        // Merge, sort by most recent, take top 10
        var merged = items
            .OrderByDescending(x => x.when)
            .Take(10)
            .Select(x => x.activity)
            .ToList();

        return merged;
    }

    [HttpGet("recent-activities")]
    public async Task<ActionResult<object>> GetRecentActivitiesPaged([FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? from = null, [FromQuery] string? to = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var agent = await agentService.GetAgentByIdAsync(userId);

        DateTime fromDate;
        if (string.Equals(from, "auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(from))
            fromDate = agent.User.CreatedAt.Date;
        else if (!DateTime.TryParse(from, out fromDate))
            return BadRequest(new { error = "Invalid from date" });

        DateTime? toDate = null;
        if (!string.IsNullOrWhiteSpace(to))
        {
            if (DateTime.TryParse(to, out var td)) toDate = td; else return BadRequest(new { error = "Invalid to date" });
        }

        var combined = new List<(DateTime when, DTOs.Agent.AgentDashboardDTO.RecentActivity activity)>();

        var attendances = await attendanceService.GetAttendanceByAgentAsync(agent);
        foreach (var att in attendances.Where(a => a.Timestamp.HasValue))
        {
            var d = att.Timestamp!.Value;
            if (d.Date < fromDate.Date || (toDate.HasValue && d.Date > toDate.Value.Date)) continue;
            combined.Add((d, new DTOs.Agent.AgentDashboardDTO.RecentActivity
            {
                Id = $"att-{att.Id}",
                Description = "Marked attendance",
                Timestamp = FormatTimeAgo(d)
            }));
        }

        if (!string.IsNullOrWhiteSpace(agent.ExternalDistributionChannelId))
        {
            var proposals = await externalClientService.GetProposalsByDistributionChannelAsync(agent.ExternalDistributionChannelId!);
            foreach (var p in proposals.Where(p => p.ProposalDate.HasValue))
            {
                var d = p.ProposalDate!.Value;
                if (d.Date < fromDate.Date || (toDate.HasValue && d.Date > toDate.Value.Date)) continue;
                var customer = string.IsNullOrWhiteSpace(p.CustomerName) ? "Client" : p.CustomerName;
                var number = string.IsNullOrWhiteSpace(p.ProposalNumber) ? "proposal" : $"proposal {p.ProposalNumber}";
                combined.Add((d, new DTOs.Agent.AgentDashboardDTO.RecentActivity
                {
                    Id = $"prop-{number}",
                    Description = $"Collected {number} for {customer}",
                    Timestamp = FormatTimeAgo(d)
                }));
            }
        }

        var sorted = combined.OrderByDescending(x => x.when).ToList();
        var total = sorted.Count;
        if (page < 1) page = 1;
        if (limit < 1) limit = 20;
        var items = sorted.Skip((page - 1) * limit).Take(limit).Select(x => x.activity).ToList();

        return Ok(new { items, page, limit, total });
    }

    private string MapPriorityToType(object priority)
    {
        if (priority == null) return "info";
    
        return priority.ToString().ToLower() switch
        {
            "urgent" => "urgent",
            "high" => "warning",
            "low" => "success", 
            _ => "info"
        };
    }

    private string FormatTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "unknown time ago";
            
        var now = DateTime.Now;
        var timeSpan = now - dateTime.Value;
        var minutes = timeSpan.TotalMinutes;
        var hours = timeSpan.TotalHours;
        var days = timeSpan.TotalDays;
        
        if (minutes < 60)
            return $"{(int)minutes} minutes ago";
        else if (hours < 24)
            return $"{(int)hours} hours ago";
        else
            return $"{(int)days} days ago";
    }

    private async Task<DTOs.Agent.GroupPerformanceDTO> BuildGroupPerformanceDto(Group group)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        var groupName = group.Name ?? "Unknown Group";
        var teamMembers = group.Agents?.ToList() ?? new List<Agent>();
        var leader = group.Leader;
        int totalGroupClients = 0;
        foreach (var member in teamMembers)
            totalGroupClients += 0;
        // Team rank among all groups
        var allGroups = await groupService.GetGroupsByManagerAsync(group.Manager);
        var groupClientCounts = new List<int>();
        foreach (var g in allGroups)
        {
            int groupClients = 0;
            foreach (var m in g.Agents ?? new List<Agent>())
                groupClients += 0;
            groupClientCounts.Add(groupClients);
        }
        groupClientCounts.Sort((a, b) => b.CompareTo(a));
        int rank = groupClientCounts.IndexOf(totalGroupClients) + 1;
        int totalGroups = groupClientCounts.Count;
        var kpis = new DTOs.Agent.GroupPerformanceDTO.KPIs
        {
            TotalClients = totalGroupClients,
            TeamMembersCount = teamMembers.Count,
            TeamRank = new DTOs.Agent.GroupPerformanceDTO.TeamRank { Rank = rank, Total = totalGroups }
        };
        // Performance trends for the last 4 weeks
        var trends = new List<DTOs.Agent.GroupPerformanceDTO.PerformanceTrend>();
        var today = DateTime.Now.Date;
        for (int i = 3; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i).StartOfWeek(DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(6);
            int weekClients = 0;
            foreach (var member in teamMembers)
                weekClients += 0;
            trends.Add(new DTOs.Agent.GroupPerformanceDTO.PerformanceTrend
            {
                Name = $"Week {4 - i}",
                Clients = weekClients
            });
        }
        // Team leader info
        DTOs.Agent.GroupPerformanceDTO.TeamMember teamLeaderDto = null;
        if (leader != null && leader.User != null)
        {
            var leaderUser = leader.User;
            int leaderClients = 0;
            int leaderRate = (int)CalculatePerformanceRate(leader);
            teamLeaderDto = new DTOs.Agent.GroupPerformanceDTO.TeamMember
            {
                Id = leaderUser.Id.ToString(),
                Name = $"{leaderUser.FirstName} {leaderUser.LastName}",
                Clients = leaderClients,
                Rate = leaderRate,
                IsTeamLeader = true
            };
        }
        // Team members list
        var teamMemberDtOs = new List<DTOs.Agent.GroupPerformanceDTO.TeamMember>();
        foreach (var member in teamMembers)
        {
            if (leader != null && member.UserId == leader.UserId) continue;
            if (member.User == null) continue; // Skip members without user data
            
            var memberUser = member.User;
            int memberClients = 0;
            int memberRate = (int)CalculatePerformanceRate(member);
            teamMemberDtOs.Add(new DTOs.Agent.GroupPerformanceDTO.TeamMember
            {
                Id = memberUser.Id.ToString(),
                Name = $"{memberUser.FirstName} {memberUser.LastName}",
                Clients = memberClients,
                Rate = memberRate,
                IsTeamLeader = false
            });
        }
        // Recent activities (clients removed) - return empty list for now
        var recentActivities = new List<DTOs.Agent.GroupPerformanceDTO.RecentActivity>();
        return new DTOs.Agent.GroupPerformanceDTO
        {
            GroupName = groupName,
            Kpis = kpis,
            PerformanceTrends = trends,
            TeamLeader = teamLeaderDto ?? new DTOs.Agent.GroupPerformanceDTO.TeamMember
            {
                Id = "",
                Name = "No Team Leader",
                Clients = 0,
                Rate = 0,
                IsTeamLeader = true
            },
            TeamMembers = teamMemberDtOs,
            RecentActivities = recentActivities
        };
    }
}