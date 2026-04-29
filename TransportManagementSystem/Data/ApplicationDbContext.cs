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
        }
    }
}