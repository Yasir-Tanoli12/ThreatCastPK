using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Models;

namespace ThreatCastPK.Database.Context
{
    public class ThreatCastDbContext : DbContext
    {
        public ThreatCastDbContext(DbContextOptions<ThreatCastDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<AttackReport> AttackReports { get; set; }
        public DbSet<AttackEvent> AttackEvents { get; set; }
        public DbSet<ThreatCampaign> ThreatCampaigns { get; set; }
        public DbSet<AlertSubscription> AlertSubscriptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DiscussionPost> DiscussionPosts { get; set; }
        public DbSet<ThreatAdvisory> ThreatAdvisories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SectorRiskScore> SectorRiskScores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>();
            });

            // Location
            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            });

            // AttackReport
            modelBuilder.Entity<AttackReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AttackType).HasConversion<string>();
                entity.Property(e => e.TargetSector).HasConversion<string>();
                entity.Property(e => e.ConfidenceTier).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Reporter)
                      .WithMany(u => u.AttackReports)
                      .HasForeignKey(e => e.ReporterId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Location)
                      .WithMany()
                      .HasForeignKey(e => e.LocationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AttackEvent
            modelBuilder.Entity<AttackEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AttackType).HasConversion<string>();
                entity.Property(e => e.TargetSector).HasConversion<string>();
                entity.Property(e => e.ConfidenceTier).HasConversion<string>();
                entity.Property(e => e.Source).HasConversion<string>();
                entity.HasOne(e => e.Location)
                      .WithMany(l => l.AttackEvents)
                      .HasForeignKey(e => e.LocationId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AttackReport)
                      .WithOne(r => r.AttackEvent)
                      .HasForeignKey<AttackEvent>(e => e.ReportId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ThreatCampaign)
                      .WithMany(c => c.AttackEvents)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ThreatCampaign
            modelBuilder.Entity<ThreatCampaign>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.AlertLevel).HasConversion<string>();
            });

            // AlertSubscription
            modelBuilder.Entity<AlertSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasOne(e => e.User)
                      .WithMany(u => u.AlertSubscriptions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AlertSubscription)
                      .WithMany(s => s.Notifications)
                      .HasForeignKey(e => e.SubscriptionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // DiscussionPost
            modelBuilder.Entity<DiscussionPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasOne(e => e.User)
                      .WithMany(u => u.DiscussionPosts)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ThreatAdvisory
            modelBuilder.Entity<ThreatAdvisory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasOne(e => e.Admin)
                      .WithMany(u => u.ThreatAdvisories)
                      .HasForeignKey(e => e.AdminId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.HasOne(e => e.Admin)
                      .WithMany(u => u.AuditLogs)
                      .HasForeignKey(e => e.AdminId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // SectorRiskScore
            modelBuilder.Entity<SectorRiskScore>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.RiskLevel).HasConversion<string>();
                entity.HasIndex(e => e.SectorName).IsUnique();
            });
        }
    }
}