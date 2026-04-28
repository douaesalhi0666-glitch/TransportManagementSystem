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
        public DbSet<Admin> Admins { get; set; }   // Nouvelle ligne

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Personnel
            modelBuilder.Entity<Personnel>()
                .ToTable("Personnel_tbl", "Security")
                .HasKey(e => e.Personnel_Id);

            // Driver
            modelBuilder.Entity<Driver>()
                .ToTable("Driver_tbl", "Security")
                .HasKey(e => e.Driver_id);

            // Bus
            modelBuilder.Entity<Bus>()
                .ToTable("Bus_tbl", "Transport")
                .HasKey(e => e.Bus_Id);

            // Trajectory
            modelBuilder.Entity<Trajectory>()
                .ToTable("Trajectory_tbl", "Transport")
                .HasKey(e => e.Trajectory_Id);

            // Admin (nouvelle entité)
            modelBuilder.Entity<Admin>()
                .ToTable("Admin_tbl", "Security")
                .HasKey(e => e.Admin_Id);
        }
    }
}