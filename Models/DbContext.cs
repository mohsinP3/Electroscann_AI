using ElectroScanAI.Models.Entities;
using Electroscann_ai.Models;
using Microsoft.EntityFrameworkCore;

namespace Electroscann_ai.Data
{
    public class ElectroscannDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ElectroscannDbContext(DbContextOptions<ElectroscannDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Electrician> Electricians { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<AIScan> AIScans { get; set; }
        public DbSet<ScanResult> ScanResults { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WireCalculation> WireCalculations { get; set; }
        public DbSet<MarketRate> MarketRates { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User <-> Electrician (1:1)
            modelBuilder.Entity<Electrician>()
                .HasOne(e => e.User)
                .WithOne(u => u.ElectricianProfile)
                .HasForeignKey<Electrician>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Electrician>()
                .HasIndex(e => e.UserId)
                .IsUnique();

            // User <-> Company (1:1)
            modelBuilder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.CompanyProfile)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // Reviews: avoid multiple cascade paths (User -> Review AND User -> Electrician -> Review)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Electrician)
                .WithMany()
                .HasForeignKey(r => r.ElectricianId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobApplications: avoid multiple cascade paths (User -> Company -> Job -> App
            // AND User -> Electrician -> App)
            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.Job)
                .WithMany()
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.Electrician)
                .WithMany()
                .HasForeignKey(a => a.ElectricianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}