using Microsoft.EntityFrameworkCore;

namespace NerdDinner.Models
{
    public class NerdDinnerContext : DbContext
    {
        public NerdDinnerContext(DbContextOptions<NerdDinnerContext> options) : base(options)
        {
        }

        public DbSet<Dinner> Dinners { get; set; }
        public DbSet<RSVP> RSVPs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Dinner>(entity =>
            {
                entity.HasKey(d => d.DinnerID);
                entity.Property(d => d.Location).HasColumnType("geography");
                entity.Ignore(d => d.LocationDetail);
                entity.HasMany(d => d.RSVPs)
                      .WithOne(r => r.Dinner)
                      .HasForeignKey(r => r.DinnerID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RSVP>(entity =>
            {
                entity.HasKey(r => r.RsvpID);
            });
        }
    }
}
