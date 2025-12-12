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
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<ReviewReply> ReviewReplies { get; set; }
        public DbSet<BusinessImage> BusinessImages { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignUsage> CampaignUsages { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<CustomerInteraction> CustomerInteractions { get; set; }

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

                entity.HasOne(r => r.Branch)
                      .WithMany(b => b.Reviews)
                      .HasForeignKey(r => r.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Ignore computed properties
                entity.Ignore(r => r.LikeCount);
                entity.Ignore(r => r.DislikeCount);
            });

            modelBuilder.Entity<ReviewReply>(entity =>
            {
                entity.HasOne(rr => rr.Review)
                      .WithMany(r => r.Replies)
                      .HasForeignKey(rr => rr.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rr => rr.User)
                      .WithMany()
                      .HasForeignKey(rr => rr.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReviewLike>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ReviewId, e.UserId }).IsUnique();

                entity.HasOne(rl => rl.Review)
                      .WithMany(r => r.Likes)
                      .HasForeignKey(rl => rl.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rl => rl.User)
                      .WithMany()
                      .HasForeignKey(rl => rl.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<BusinessImage>(entity =>
            {
                entity.HasOne(bi => bi.Business)
                      .WithMany(b => b.Images)
                      .HasForeignKey(bi => bi.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserPhoto>(entity =>
            {
                entity.HasOne(up => up.User)
                      .WithMany(u => u.Photos)
                      .HasForeignKey(up => up.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Business)
                      .WithMany()
                      .HasForeignKey(p => p.BusinessId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasOne(s => s.User)
                      .WithMany()
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Business)
                      .WithMany()
                      .HasForeignKey(s => s.BusinessId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationPreference>(entity =>
            {
                entity.HasOne(np => np.User)
                      .WithMany()
                      .HasForeignKey(np => np.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Receiver)
                      .WithMany()
                      .HasForeignKey(m => m.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.RelatedBusiness)
                      .WithMany()
                      .HasForeignKey(m => m.RelatedBusinessId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.RelatedReservation)
                      .WithMany()
                      .HasForeignKey(m => m.RelatedReservationId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasOne(c => c.User1)
                      .WithMany()
                      .HasForeignKey(c => c.User1Id)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.User2)
                      .WithMany()
                      .HasForeignKey(c => c.User2Id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.Business)
                      .WithMany(b => b.Events)
                      .HasForeignKey(e => e.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EventAttendee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();

                entity.HasOne(ea => ea.Event)
                      .WithMany(e => e.Attendees)
                      .HasForeignKey(ea => ea.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ea => ea.User)
                      .WithMany()
                      .HasForeignKey(ea => ea.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.HasOne(c => c.Business)
                      .WithMany(b => b.Campaigns)
                      .HasForeignKey(c => c.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CampaignUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CampaignId, e.UserId });

                entity.HasOne(cu => cu.Campaign)
                      .WithMany(c => c.Usages)
                      .HasForeignKey(cu => cu.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cu => cu.User)
                      .WithMany()
                      .HasForeignKey(cu => cu.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasOne(b => b.Business)
                      .WithMany()
                      .HasForeignKey(b => b.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasOne(e => e.Business)
                      .WithMany()
                      .HasForeignKey(e => e.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Branch)
                      .WithMany()
                      .HasForeignKey(e => e.BranchId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CustomerInteraction>(entity =>
            {
                entity.HasOne(ci => ci.Business)
                      .WithMany()
                      .HasForeignKey(ci => ci.BusinessId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Customer)
                      .WithMany()
                      .HasForeignKey(ci => ci.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.RelatedReservation)
                      .WithMany()
                      .HasForeignKey(ci => ci.RelatedReservationId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.RelatedReview)
                      .WithMany()
                      .HasForeignKey(ci => ci.RelatedReviewId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.RelatedMessage)
                      .WithMany()
                      .HasForeignKey(ci => ci.RelatedMessageId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasOne(r => r.Branch)
                      .WithMany(b => b.Reservations)
                      .HasForeignKey(r => r.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.Property(r => r.Amount)
                      .HasPrecision(18, 2);
            });

            // Decimal precision ayarları
            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.Property(c => c.DiscountPercentage)
                      .HasPrecision(18, 2);
                entity.Property(c => c.MinimumPurchaseAmount)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<CampaignUsage>(entity =>
            {
                entity.Property(cu => cu.DiscountApplied)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.Salary)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Price)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount)
                      .HasPrecision(18, 2);
            });
        }
    }
}