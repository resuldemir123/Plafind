using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Plafind.Models;

namespace Plafind.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Business> Businesses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<UserPhoto> UserPhotos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.BusinessId }).IsUnique();
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Business)
                      .WithMany(b => b.Reviews)
                      .HasForeignKey(r => r.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasOne(f => f.Business)
                      .WithMany(b => b.Favorites)
                      .HasForeignKey(f => f.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.User)
                      .WithMany(u => u.Favorites)
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<News>(entity =>
            {
                entity.HasOne(n => n.Author)
                      .WithMany()
                      .HasForeignKey(n => n.AuthorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasOne(up => up.User)
                      .WithMany()
                      .HasForeignKey(up => up.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Business>(entity =>
            {
                // Kategori ilişkisi
                entity.HasOne(b => b.Category)
                      .WithMany(c => c.Businesses)
                      .HasForeignKey(b => b.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                // İşletme sahibi (BusinessOwner)
                entity.HasOne(b => b.Owner)
                      .WithMany(u => u.OwnedBusinesses)
                      .HasForeignKey(b => b.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserPhoto>(entity =>
            {
                entity.HasOne(up => up.User)
                      .WithMany(u => u.Photos)
                      .HasForeignKey(up => up.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}