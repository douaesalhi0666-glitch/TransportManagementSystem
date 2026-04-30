using Microsoft.EntityFrameworkCore;
using TransportManagementSystem.Models;

namespace TransportManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Entités existantes
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Trajectory> Trajectories { get; set; }
        public DbSet<Admin> Admin_tbl { get; set; }
        public DbSet<TrajectoryStop> TrajectoryStops { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PersonnelTrajectoryAssignment> PersonnelTrajectoryAssignments { get; set; }

        // Nouvelle entité pour les arrêts suggérés par clustering
        public DbSet<SuggestedStop> SuggestedStops { get; set; }

        // DTO pour les procédures stockées (si vous les utilisez)
        // (commentez ou supprimez selon votre cas)
        // public DbSet<PersonnelDto> PersonnelDtos { get; set; }
        // etc.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurations des tables existantes
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

            // Configuration de la nouvelle table SuggestedStop
            modelBuilder.Entity<SuggestedStop>(entity =>
            {
                entity.ToTable("SuggestedStops_tbl", "Transport");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Latitude).HasColumnType("decimal(10,8)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(11,8)");
            });

            // Relations existantes (inchangées)
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Driver_AssignedBus)
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