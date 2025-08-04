using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // User-related entities
    public DbSet<User> Users { get; set; }
    public DbSet<Manager> Managers { get; set; }
    public DbSet<Agent> Agents { get; set; }
    
    // Group and organization
    public DbSet<Group> Groups { get; set; }
    
    // Client management
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientsCollected> ClientsCollected { get; set; }
    
    // Attendance tracking
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<AttendanceTimeframe> AttendanceTimeframes { get; set; }
    
    // System entities
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnType("bigint");
            entity.Property(e => e.Email).HasColumnType("varchar(255)");
            entity.Property(e => e.WorkId).HasColumnType("varchar(50)");
            entity.Property(e => e.FirstName).HasColumnType("varchar(100)");
            entity.Property(e => e.LastName).HasColumnType("varchar(100)");
            entity.Property(e => e.PhoneNumber).HasColumnType("varchar(20)");
            entity.Property(e => e.NationalId).HasColumnType("varchar(50)");
            entity.Property(e => e.PasswordHash).HasColumnType("varchar(60)");
            entity.Property(e => e.Role).HasColumnType("varchar(20)").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.Active).HasColumnType("boolean");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.WorkId).IsUnique();
            entity.HasIndex(e => e.NationalId).IsUnique();
        });

        // Configure Manager entity
        builder.Entity<Manager>(entity =>
        {
            entity.ToTable("managers");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnType("bigint");
            entity.Property(e => e.CreatedById).HasColumnType("bigint");
            entity.Property(e => e.Department).HasColumnType("varchar(100)");
            entity.HasOne(e => e.User)
                .WithOne(e => e.Manager)
                .HasForeignKey<Manager>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedBy)
                .WithMany(e => e.CreatedManagers)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Agent entity
        builder.Entity<Agent>(entity =>
        {
            entity.ToTable("agents");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnType("bigint");
            entity.Property(e => e.ManagerId).HasColumnType("bigint");
            entity.Property(e => e.GroupId).HasColumnType("bigint");
            entity.Property(e => e.AgentType).HasColumnType("varchar(20)").HasConversion<string>();
            entity.Property(e => e.Sector).HasColumnType("varchar(100)");
            entity.HasOne(e => e.User)
                .WithOne(e => e.Agent)
                .HasForeignKey<Agent>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.Agents)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Group)
                .WithMany(e => e.Agents)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Group entity
        builder.Entity<Group>(entity =>
        {
            entity.ToTable("agent_groups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.Groups)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Leader)
                .WithMany(e => e.LedGroups)
                .HasForeignKey(e => e.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Client entity
        builder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.NationalId).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasOne(e => e.Agent)
                .WithMany(e => e.Clients)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Attendance entity
        builder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Agent)
                .WithMany(e => e.Attendances)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AttendanceTimeframe entity
        builder.Entity<AttendanceTimeframe>(entity =>
        {
            entity.ToTable("attendance_timeframes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.AttendanceTimeframes)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Notification entity
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Sender)
                .WithMany(e => e.SentNotifications)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Recipient)
                .WithMany(e => e.ReceivedNotifications)
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AuditLog entity
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ClientsCollected entity
        builder.Entity<ClientsCollected>(entity =>
        {
            entity.ToTable("clients_collected");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Agent)
                .WithMany(e => e.ClientsCollectedRecords)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}