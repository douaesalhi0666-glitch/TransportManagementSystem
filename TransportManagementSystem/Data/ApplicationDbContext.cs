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
        public DbSet<Admin> Admin_tbl { get; set; }  // ← ADD THIS LINE

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
        }
    }
}