using RockGym.Models;
using Microsoft.EntityFrameworkCore;

namespace RockGym.Services
{
    public class RockGymContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Entrance> Entrances { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PurchaseHistory> PurchaseHistories { get; set; }
        public DbSet<QrCard> QrCards { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = "server=127.0.0.1;port=3306;database=rock_gym;user=root;password=";

                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Surname).HasColumnName("surname").IsRequired();
                entity.Property(e => e.Login).HasColumnName("login").IsRequired();
                entity.Property(e => e.Password).HasColumnName("password").IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").IsRequired();
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.ProfilePicture).HasColumnName("profile_picture");
                entity.Property(e => e.RoleId).HasColumnName("role_id");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Entrance>(entity =>
            {
                entity.ToTable("entrances");
                entity.HasKey(e => e.EntranceId);
                entity.Property(e => e.EntranceId).HasColumnName("entrance_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.DateOfEntry).HasColumnName("date_of_entry");
                entity.Property(e => e.StartTime).HasColumnName("start_time");
                entity.Property(e => e.EndTime).HasColumnName("end_time");
                entity.Property(e => e.TimeSpent).HasColumnName("time_spent");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Entrances)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.ToTable("event_participants");
                entity.HasKey(e => new { e.EventId, e.ParticipantId });
                entity.Property(e => e.EventId).HasColumnName("event_id");
                entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
                entity.Property(e => e.DateOfRegistration).HasColumnName("date_of_registration");

                entity.HasOne(d => d.Event)
                    .WithMany(p => p.EventParticipants)
                    .HasForeignKey(d => d.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Participant)
                    .WithMany(p => p.EventParticipations)
                    .HasForeignKey(d => d.ParticipantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PurchaseHistory>(entity =>
            {
                entity.ToTable("purchase_history");
                entity.HasKey(e => e.PurchaseId);
                entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
                entity.Property(e => e.CustomerId).HasColumnName("customer_id");
                entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
                entity.Property(e => e.OfferId).HasColumnName("offer_id");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.CustomerPurchases)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.EmployeePurchases)
                    .HasForeignKey(d => d.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Offer)
                    .WithMany(p => p.PurchaseHistories)
                    .HasForeignKey(d => d.OfferId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Offer>(entity =>
            {
                entity.ToTable("offers").HasKey(e => e.OfferId);
                entity.Property(e => e.OfferId).HasColumnName("offer_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.Duration).HasColumnName("duration");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("events").HasKey(e => e.EventId);
                entity.Property(e => e.EventId).HasColumnName("event_id");
                entity.Property(e => e.InstructorId).HasColumnName("instructor_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.EventColor).HasColumnName("event_color").IsRequired();
                entity.Property(e => e.ParticipantsLimit).HasColumnName("participants_limit");
                entity.Property(e => e.OfferId).HasColumnName("offer_id");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");

                entity.HasOne(d => d.Instructor).WithMany(p => p.CommandedEvents).HasForeignKey(d => d.InstructorId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(d => d.Offer).WithMany(p => p.Events).HasForeignKey(d => d.OfferId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications").HasKey(e => e.NotificationId);
                entity.Property(e => e.NotificationId).HasColumnName("notification_id");
                entity.Property(e => e.CreatorId).HasColumnName("creator_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");

                entity.HasOne(d => d.Creator)
                    .WithMany()
                    .HasForeignKey(d => d.CreatorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<QrCard>(entity =>
            {
                entity.ToTable("qr_cards").HasKey(e => e.CardId);
                entity.Property(e => e.CardId).HasColumnName("card_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.QrCode).HasColumnName("qr_code");
                entity.Property(e => e.DateOfCreation).HasColumnName("date_of_creation");

                entity.HasOne(d => d.User).WithMany(p => p.QrCards).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}