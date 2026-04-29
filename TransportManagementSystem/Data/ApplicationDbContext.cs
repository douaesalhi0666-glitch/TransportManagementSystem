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

        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Trajectory> Trajectories { get; set; }
        public DbSet<Admin> Admin_tbl { get; set; }
        public DbSet<TrajectoryStop> TrajectoryStops { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PersonnelTrajectoryAssignment> PersonnelTrajectoryAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tables et clés primaires
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

            // ========== CONFIGURATION DES RELATIONS ==========

            // Driver → Bus (bus assigné)
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Driver_AssignedBus)
                .WithMany() // Pas de navigation inverse explicite (un bus peut avoir plusieurs chauffeurs historiquement)
                .HasForeignKey(d => d.Driver_AssignedBusId)
                .OnDelete(DeleteBehavior.SetNull);

            // Bus → Driver (chauffeur actuel – optionnel, si la propriété CurrentDriver existe dans Bus)
            // Décommentez cette section si vous avez ajouté public Driver? CurrentDriver { get; set; } dans Bus.cs
            /*
            modelBuilder.Entity<Bus>()
                .HasOne(b => b.CurrentDriver)
                .WithMany()
                .HasForeignKey(b => b.Bus_CurrentDriverId)
                .OnDelete(DeleteBehavior.SetNull);
            */

            // PersonnelTrajectoryAssignment → Personnel
            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Personnel)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_PersonnelId)
                .OnDelete(DeleteBehavior.Restrict);

            // PersonnelTrajectoryAssignment → Trajectory
            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Trajectory)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_TrajectoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // PersonnelTrajectoryAssignment → Stop (optionnel)
            modelBuilder.Entity<PersonnelTrajectoryAssignment>()
                .HasOne(pt => pt.Stop)
                .WithMany()
                .HasForeignKey(pt => pt.PTA_StopId)
                .OnDelete(DeleteBehavior.Restrict);

            // Alert → Bus
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Bus)
                .WithMany()
                .HasForeignKey(a => a.Alert_BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Alert → Personnel
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Personnel)
                .WithMany()
                .HasForeignKey(a => a.Alert_PersonnelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Alert → Trajectory
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Trajectory)
                .WithMany()
                .HasForeignKey(a => a.Alert_TrajectoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}