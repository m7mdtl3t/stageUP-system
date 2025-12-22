using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Models;

namespace VivuqeQRSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Senior> Seniors { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Senior entity
            modelBuilder.Entity<Senior>(entity =>
            {
                entity.HasKey(e => e.SeniorId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.QrUrl).HasMaxLength(500);
                
                // Indexes for performance
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => e.EventId);
                entity.HasIndex(e => e.ShareToken);

                entity.HasMany(e => e.Guests)
                      .WithOne(e => e.Senior)
                      .HasForeignKey(e => e.SeniorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Guest entity
            modelBuilder.Entity<Guest>(entity =>
            {
                entity.HasKey(e => e.GuestId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                
                // Indexes for performance
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => e.IsAttended);
                entity.HasIndex(e => e.TicketToken);

                entity.HasOne(e => e.Senior)
                      .WithMany(e => e.Guests)
                      .HasForeignKey(e => e.SeniorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Event entity
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.IsActive);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();
            });
        }
    }
}
