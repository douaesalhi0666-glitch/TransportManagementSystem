using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Models;
using TransportManagementSystem.Services;

namespace TransportManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ================================================
        // ENTITÉS
        // ================================================
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<DriverPerformance> DriverPerformance_tbl { get; set; }
        public DbSet<Trajectory> Trajectories { get; set; }
        public DbSet<Admin> Admin_tbl { get; set; }
        public DbSet<DriverMission> DriverMissions_tbl { get; set; }
        public DbSet<BusTrajectoryAssignment> BusTrajectoryAssignments { get; set; }
        public DbSet<TrajectorySchedule> TrajectorySchedules { get; set; }
        public DbSet<TrajectoryStop> TrajectoryStops { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PersonnelTrajectoryAssignment> PersonnelTrajectoryAssignments { get; set; }
        public DbSet<SuggestedStop> SuggestedStops { get; set; }
        public DbSet<MotorizationRequest> MotorizationRequests { get; set; }
        // NOUVEAU
        public DbSet<PersonnelBusAssignment> PersonnelBusAssignments { get; set; }
        public DbSet<AnomalyLog> AnomalyLogs { get; set; }
        public DbSet<FuelConsumption> FuelConsumptions { get; set; }

        public DbSet<TransportManagementSystem.Models.BusLocationHistory> BusLocationHistory { get; set; }
        public DbSet<ArrivalHistory> ArrivalHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================================================
            // CONFIGURATIONS DES TABLES
            // ================================================

            modelBuilder.Entity<Personnel>(entity =>
            {
                entity.ToTable("Personnel_tbl", "Security");
                entity.HasKey(e => e.Personnel_Id);
            });

            modelBuilder.Entity<Driver>(entity =>
            {
                entity.ToTable("Driver_tbl", "Security");
                entity.HasKey(e => e.Driver_id);
            });

            modelBuilder.Entity<Bus>(entity =>
            {
                entity.ToTable("Bus_tbl", "Transport");
                entity.HasKey(e => e.Bus_Id);
            });

            modelBuilder.Entity<Trajectory>(entity =>
            {
                entity.ToTable("Trajectory_tbl", "Transport");
                entity.HasKey(e => e.Trajectory_Id);
            });

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admin_tbl", "Security");
                entity.HasKey(e => e.Admin_Id);
            });

            modelBuilder.Entity<TrajectoryStop>(entity =>
            {
                entity.ToTable("TrajectoryStop_tbl", "Transport");
                entity.HasKey(e => e.TS_Id);
                entity.Property(e => e.TS_Latitude).HasColumnType("decimal(10,8)");
                entity.Property(e => e.TS_Longitude).HasColumnType("decimal(11,8)");
            });

            modelBuilder.Entity<Alert>(entity =>
            {
                entity.ToTable("Alert_tbl", "Service");
                entity.HasKey(e => e.Alert_Id);
            });

            modelBuilder.Entity<PersonnelTrajectoryAssignment>(entity =>
            {
                entity.ToTable("PersonnelTrajectoryAssignments_tbl", "Assignment");
                entity.HasKey(e => e.PTA_Id);
            });

            modelBuilder.Entity<SuggestedStop>(entity =>
            {
                entity.ToTable("SuggestedStops_tbl", "Transport");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Latitude).HasColumnType("decimal(10,8)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(11,8)");
            });

            // ================================================
            // CONFIGURATION DES DEMANDES DE MOTORISATION
            // ================================================
            modelBuilder.Entity<MotorizationRequest>(entity =>
            {
                entity.ToTable("MotorizationRequests_tbl", "Service");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.HasOne(e => e.Personnel)
                      .WithMany()
                      .HasForeignKey(e => e.PersonnelId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ================================================
            // CONFIGURATION DE PersonnelBusAssignment
            // ================================================
            modelBuilder.Entity<PersonnelBusAssignment>(entity =>
            {
                entity.ToTable("PersonnelBusAssignment_tbl", "Assignment");
                entity.HasKey(e => e.PBA_Id);
                entity.HasIndex(e => new { e.PBA_PersonnelId, e.PBA_BusId, e.PBA_Status })
                      .HasDatabaseName("IX_PBA_PersonnelBus_Status");
                entity.HasOne(e => e.Personnel)
                      .WithMany()
                      .HasForeignKey(e => e.PBA_PersonnelId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Bus)
                      .WithMany()
                      .HasForeignKey(e => e.PBA_BusId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ================================================
            // RELATIONS EXISTANTES
            // ================================================

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.AssignedBus)
                .WithMany()
                .HasForeignKey(d => d.Driver_AssignedBusId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Personnel)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_PersonnelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Trajectory)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_TrajectoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Stop)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_StopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Bus)
                .WithMany()
                .HasForeignKey(a => a.Alert_BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Personnel)
                .WithMany()
                .HasForeignKey(a => a.Alert_PersonnelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Trajectory)
                .WithMany()
                .HasForeignKey(a => a.Alert_TrajectoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}