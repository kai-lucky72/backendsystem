using backend.DTOs.Agent;
using backend.DTOs.Client;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IAttendanceService _attendanceService;
    private readonly IClientsCollectedService _clientsCollectedService;
    private readonly IClientService _clientService;
    private readonly IAttendanceTimeframeService _attendanceTimeframeService;
    private readonly IGroupService _groupService;
    private readonly INotificationService _notificationService;

    public AgentController(
        IAgentService agentService,
        IAttendanceService attendanceService,
        IClientsCollectedService clientsCollectedService,
        IClientService clientService,
        IAttendanceTimeframeService attendanceTimeframeService,
        IGroupService groupService,
        INotificationService notificationService)
    {
        _agentService = agentService;
        _attendanceService = attendanceService;
        _clientsCollectedService = clientsCollectedService;
        _clientService = clientService;
        _attendanceTimeframeService = attendanceTimeframeService;
        _groupService = groupService;
        _notificationService = notificationService;
    }

    [HttpPost("attendance")]
    public async Task<ActionResult> MarkAttendance([FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var location = body.GetValueOrDefault("location", "");
        var sector = body.GetValueOrDefault("sector", "");
        var attendance = await _attendanceService.MarkAttendanceAsync(agent, location, sector);
        var time = attendance.Timestamp.ToString("HH:mm");
        var date = attendance.Timestamp.ToString("yyyy-MM-dd");
        var attendanceInfo = new Dictionary<string, object>
        {
            ["date"] = date,
            ["time"] = time,
            ["status"] = "Present"
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
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var all = await _attendanceService.GetAttendanceByAgentAsync(agent);
        int present = 0, late = 0, absent = 0;
        var records = new List<Dictionary<string, object>>();
        foreach (var att in all)
        {
            var status = att.Timestamp.TimeOfDay > new TimeSpan(8, 0, 0) ? "late" : "present";
            if (status == "present") present++; else late++;
            records.Add(new Dictionary<string, object>
            {
                ["id"] = $"rec-{att.Id}",
                ["date"] = att.Timestamp.ToString("yyyy-MM-dd"),
                ["time"] = att.Timestamp.ToString("HH:mm"),
                ["location"] = att.Location,
                ["sector"] = att.Sector,
                ["status"] = status
            });
        }
        int totalDays = all.Select(a => a.Timestamp.Date).Distinct().Count();
        int attendanceRate = totalDays > 0 ? (int)((present / (double)totalDays) * 100) : 0;
        var response = new Dictionary<string, object>
        {
            ["attendanceRate"] = attendanceRate,
            ["presentCount"] = present,
            ["lateCount"] = late,
            ["absentCount"] = absent,
            ["records"] = records
        };
        return Ok(response);
    }

    [HttpGet("attendance/timeframe")]
    public async Task<ActionResult> GetAttendanceTimeframe()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var manager = agent.Manager;
        var timeframe = await _attendanceTimeframeService.GetTimeframeByManagerAsync(manager);
        string start = "09:00", end = "10:00";
        if (timeframe != null)
        {
            start = timeframe.StartTime.ToString();
            end = timeframe.EndTime.ToString();
        }
        return Ok(new Dictionary<string, string> { ["startTime"] = start, ["endTime"] = end });
    }

    [HttpGet("attendance/status")]
    public async Task<ActionResult> GetAttendanceStatus()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        if (agent == null)
            return NotFound(new { error = $"Agent not found for user {userId}" });
        bool hasMarkedToday = await _attendanceService.HasMarkedAttendanceTodayAsync(agent);
        var lastAttendance = await _attendanceService.GetLastAttendanceTimeAsync(agent);
        return Ok(new Dictionary<string, object>
        {
            ["hasMarkedToday"] = hasMarkedToday,
            ["lastAttendanceTime"] = lastAttendance?.ToString() ?? ""
        });
    }

    [HttpPost("clients-collected")]
    public async Task<ActionResult> CollectClient([FromBody] Dictionary<string, object> clientData)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var clientsCollected = await _clientsCollectedService.CollectClientAsync(agent, clientData);
        return Ok(clientsCollected);
    }

    [HttpGet("clients-collected")]
    public async Task<ActionResult> GetClientsCollected([FromQuery] DateTime? date = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        IEnumerable<ClientsCollected> clients;
        if (date.HasValue)
        {
            var startOfDay = date.Value.Date;
            var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);
            clients = await _clientsCollectedService.GetClientsByAgentAndDateRangeAsync(agent, startOfDay, endOfDay);
        }
        else
        {
            clients = await _clientsCollectedService.GetClientsByAgentAsync(agent);
        }
        return Ok(clients);
    }

    // --- FULL DTO LOGIC FOR /performance ---
    [HttpGet("performance")]
    public async Task<ActionResult> GetPerformance([FromQuery] string period = "weekly", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
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
        var attendances = (await _attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, start, end)).ToList();
        var totalClients = await _clientService.CountClientsByAgentAndDateRangeAsync(agent, start, end);
        var chartData = await GenerateChartData(agent, start, end, attendances);
        var stats = CalculatePerformanceStats(agent, start, end, attendances, totalClients, chartData);
        var trends = await CalculateTrends(agent, period, chartData);
        var agentInfo = new DTOs.Agent.AgentPerformanceDTO.AgentInfo
        {
            Id = $"agt-{agent.UserId:D3}",
            FirstName = agent.User.FirstName,
            LastName = agent.User.LastName,
            Email = agent.User.Email,
            WorkId = agent.User.WorkId,
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
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        bool attendanceMarked = await _attendanceService.HasMarkedAttendanceTodayAsync(agent);
        var today = DateTime.Now;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
        var clientsThisMonth = await _clientService.CountClientsByAgentAndDateRangeAsync(agent, startOfMonth, endOfMonth);
        var totalClients = await _clientService.CountClientsByAgentAsync(agent);
        double performanceRate = await CalculatePerformanceRate(agent);
        var recentActivities = await GetRecentActivities(agent);
        var dto = new DTOs.Agent.AgentDashboardDTO
        {
            AttendanceMarked = attendanceMarked,
            ClientsThisMonth = (int)clientsThisMonth,
            TotalClients = (int)totalClients,
            PerformanceRate = performanceRate,
            RecentActivities = recentActivities
        };
        if (agent.AgentType == Agent.AgentType.Sales && agent.Group != null)
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
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        if (agent.AgentType != Agent.AgentType.Sales || agent.Group == null)
            return StatusCode(403);
        var group = agent.Group;
        var dto = await BuildGroupPerformanceDTO(group);
        return Ok(dto);
    }

    // --- FULL DTO LOGIC FOR /notifications ---
    [HttpGet("notifications")]
    public async Task<ActionResult> GetAgentNotifications()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var notifications = await _notificationService.GetNotificationsByRecipientAsync(agent.User);
        var notificationList = notifications.Select(n => new DTOs.Notification.NotificationMessage
        {
            Id = n.Id,
            SenderId = n.Sender?.Id ?? 0,
            SenderWorkId = n.Sender?.WorkId ?? string.Empty,
            SenderName = n.Sender != null ? $"{n.Sender.FirstName} {n.Sender.LastName}" : string.Empty,
            Message = n.Message,
            Timestamp = n.SentAt,
            Type = n.Type // Map as needed
        }).ToList();
        return Ok(new { notifications = notificationList });
    }

    [HttpPost("clients")]
    public async Task<ActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        if (!await _attendanceService.HasMarkedAttendanceTodayAsync(agent))
        {
            return StatusCode(403, new { message = "You must mark attendance for today before adding clients." });
        }
        var client = await _clientService.CreateClientAsync(request, agent, agent.User);
        var clientDTO = _clientService.MapToDTO(client);
        return StatusCode(201, clientDTO);
    }

    [HttpGet("clients")]
    public async Task<ActionResult> GetClients()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var clients = await _clientService.GetClientsByAgentAsync(agent);
        var clientDTOs = _clientService.MapToDTOList(clients);
        return Ok(clientDTOs);
    }

    [HttpGet("clients/{id}")]
    public async Task<ActionResult> GetClientById(long id)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var client = await _clientService.GetClientByIdAsync(id);
        if (client.Agent.UserId != agent.UserId)
            return StatusCode(403);
        var clientDTO = _clientService.MapToDTO(client);
        return Ok(clientDTO);
    }

    [HttpGet("clients/search")]
    public async Task<ActionResult> SearchClient([FromQuery] string nationalId = null, [FromQuery] string phoneNumber = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        Client client = null;
        if (!string.IsNullOrEmpty(nationalId))
            client = await _clientService.GetClientByNationalIdAsync(nationalId);
        else if (!string.IsNullOrEmpty(phoneNumber))
            client = await _clientService.GetClientByPhoneNumberAsync(phoneNumber);
        else
            return BadRequest();
        if (client.Agent.UserId != agent.UserId)
            return StatusCode(403);
        var clientDTO = _clientService.MapToDTO(client);
        return Ok(clientDTO);
    }

    [HttpGet("clients/date-range")]
    public async Task<ActionResult> GetClientsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var clients = await _clientService.GetClientsByAgentAndDateRangeAsync(agent, startDate, endDate);
        var clientDTOs = _clientService.MapToDTOList(clients);
        return Ok(clientDTOs);
    }

    // --- PRIVATE HELPERS ---
    private async Task<List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint>> GenerateChartData(Agent agent, DateTime start, DateTime end, List<Attendance> attendances)
    {
        var chartData = new List<DTOs.Agent.AgentPerformanceDTO.ChartDataPoint>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var startOfDay = date;
            var endOfDay = date.AddDays(1).AddSeconds(-1);
            var dayAttendances = attendances.Where(a => a.Timestamp.Date == date).ToList();
            bool hasAttendance = dayAttendances.Any();
            int present = 0, late = 0, absent = hasAttendance ? 0 : 1;
            foreach (var att in dayAttendances)
            {
                if (att.Timestamp.TimeOfDay > new TimeSpan(8, 0, 0))
                    late++;
                else
                    present++;
            }
            var clientCount = (await _clientService.GetClientsByAgentAndDateRangeAsync(agent, startOfDay, endOfDay)).Count();
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
        double clientCollectionRate = totalDays > 0 ? (double)totalClients / totalDays * 100 : 0;
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
        var attendances = (await _attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, startDate, endDate)).ToList();
        var totalClients = await _clientService.CountClientsByAgentAndDateRangeAsync(agent, startDate, endDate);
        var currentPeriodChartData = await GenerateChartData(agent, startDate, endDate, attendances);
        var currentPeriodStats = CalculatePerformanceStats(agent, startDate, endDate, attendances, totalClients, currentPeriodChartData);
        trends.Add(new DTOs.Agent.AgentPerformanceDTO.PerformanceTrend
        {
            Name = "Current Period",
            Present = currentPeriodStats.PresentCount,
            Late = currentPeriodStats.LateCount,
            Absent = currentPeriodStats.AbsentCount,
            TotalClients = currentPeriodStats.TotalClients,
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

    private async Task<double> CalculatePerformanceRate(Agent agent)
    {
        var today = DateTime.Now.Date;
        var thirtyDaysAgo = today.AddDays(-30);
        int daysWithClients = 0;
        for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
        {
            var startOfDay = date;
            var endOfDay = date.AddDays(1).AddSeconds(-1);
            var clientsOnDay = (await _clientService.GetClientsByAgentAndDateRangeAsync(agent, startOfDay, endOfDay)).Count();
            if (clientsOnDay > 0)
                daysWithClients++;
        }
        return (double)daysWithClients / 30 * 100;
    }

    private async Task<List<DTOs.Agent.AgentDashboardDTO.RecentActivity>> GetRecentActivities(Agent agent)
    {
        var recentClients = (await _clientService.GetRecentClientsByAgentAsync(agent, 5)).ToList();
        var activities = new List<DTOs.Agent.AgentDashboardDTO.RecentActivity>();
        foreach (var client in recentClients)
        {
            var creationTime = client.CreatedAt;
            var timeDescription = FormatTimeAgo(creationTime);
            activities.Add(new DTOs.Agent.AgentDashboardDTO.RecentActivity
            {
                Id = $"act-{client.Id}",
                Description = $"Added new client: {client.FullName}",
                Timestamp = timeDescription
            });
        }
        return activities;
    }

    private string FormatTimeAgo(DateTime dateTime)
    {
        var now = DateTime.Now;
        var minutes = (now - dateTime).TotalMinutes;
        var hours = (now - dateTime).TotalHours;
        var days = (now - dateTime).TotalDays;
        if (minutes < 60)
            return $"{(int)minutes} minutes ago";
        else if (hours < 24)
            return $"{(int)hours} hours ago";
        else
            return $"{(int)days} days ago";
    }

    private async Task<DTOs.Agent.GroupPerformanceDTO> BuildGroupPerformanceDTO(Group group)
    {
        var groupName = group.Name;
        var teamMembers = group.Agents.ToList();
        var leader = group.Leader;
        int totalGroupClients = 0;
        foreach (var member in teamMembers)
            totalGroupClients += (int)await _clientService.CountClientsByAgentAsync(member);
        // Team rank among all groups
        var allGroups = await _groupService.GetGroupsByManagerAsync(group.Manager);
        var groupClientCounts = new List<int>();
        foreach (var g in allGroups)
        {
            int groupClients = 0;
            foreach (var m in g.Agents)
                groupClients += (int)await _clientService.CountClientsByAgentAsync(m);
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
        // Performance trends for last 4 weeks
        var trends = new List<DTOs.Agent.GroupPerformanceDTO.PerformanceTrend>();
        var today = DateTime.Now.Date;
        for (int i = 3; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i).StartOfWeek(DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(6);
            int weekClients = 0;
            foreach (var member in teamMembers)
                weekClients += (await _clientService.GetClientsByAgentAndDateRangeAsync(member, weekStart, weekEnd)).Count();
            trends.Add(new DTOs.Agent.GroupPerformanceDTO.PerformanceTrend
            {
                Name = $"Week {4 - i}",
                Clients = weekClients
            });
        }
        // Team leader info
        DTOs.Agent.GroupPerformanceDTO.TeamMember teamLeaderDTO = null;
        if (leader != null)
        {
            var leaderUser = leader.User;
            int leaderClients = (int)await _clientService.CountClientsByAgentAsync(leader);
            int leaderRate = (int)await CalculatePerformanceRate(leader);
            teamLeaderDTO = new DTOs.Agent.GroupPerformanceDTO.TeamMember
            {
                Id = leaderUser.Id.ToString(),
                Name = $"{leaderUser.FirstName} {leaderUser.LastName}",
                Clients = leaderClients,
                Rate = leaderRate,
                IsTeamLeader = true
            };
        }
        // Team members list
        var teamMemberDTOs = new List<DTOs.Agent.GroupPerformanceDTO.TeamMember>();
        foreach (var member in teamMembers)
        {
            if (leader != null && member.UserId == leader.UserId) continue;
            var memberUser = member.User;
            int memberClients = (int)await _clientService.CountClientsByAgentAsync(member);
            int memberRate = (int)await CalculatePerformanceRate(member);
            teamMemberDTOs.Add(new DTOs.Agent.GroupPerformanceDTO.TeamMember
            {
                Id = memberUser.Id.ToString(),
                Name = $"{memberUser.FirstName} {memberUser.LastName}",
                Clients = memberClients,
                Rate = memberRate,
                IsTeamLeader = false
            });
        }
        // Recent activities from last 5 clients added by any group member
        var allRecentClients = new List<Client>();
        foreach (var member in teamMembers)
            allRecentClients.AddRange(await _clientService.GetRecentClientsByAgentAsync(member, 5));
        allRecentClients = allRecentClients.OrderByDescending(c => c.CreatedAt).ToList();
        var recentActivities = new List<DTOs.Agent.GroupPerformanceDTO.RecentActivity>();
        int maxActivities = Math.Min(5, allRecentClients.Count);
        for (int i = 0; i < maxActivities; i++)
        {
            var client = allRecentClients[i];
            var timeDescription = FormatTimeAgo(client.CreatedAt);
            recentActivities.Add(new DTOs.Agent.GroupPerformanceDTO.RecentActivity
            {
                Id = $"act-{client.Id}",
                Description = $"Added client: {client.FullName}",
                Timestamp = timeDescription
            });
        }
        return new DTOs.Agent.GroupPerformanceDTO
        {
            GroupName = groupName,
            Kpis = kpis,
            PerformanceTrends = trends,
            TeamLeader = teamLeaderDTO,
            TeamMembers = teamMemberDTOs,
            RecentActivities = recentActivities
        };
    }
}