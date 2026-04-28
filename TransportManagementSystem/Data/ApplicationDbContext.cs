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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Personnel table mapping - sans lambda
            modelBuilder.Entity<Personnel>()
                .ToTable("Personnel_tbl", "Security")
                .HasKey(e => e.Personnel_Id);  // e est autorisé, ce n'est pas "entity"

            // Driver table mapping
            modelBuilder.Entity<Driver>()
                .ToTable("Driver_tbl", "Security")
                .HasKey(e => e.Driver_id);

            // Bus table mapping
            modelBuilder.Entity<Bus>()
                .ToTable("Bus_tbl", "Transport")
                .HasKey(e => e.Bus_Id);

            // Trajectory table mapping
            modelBuilder.Entity<Trajectory>()
                .ToTable("Trajectory_tbl", "Transport")
                .HasKey(e => e.Trajectory_Id);
        }
    }
}