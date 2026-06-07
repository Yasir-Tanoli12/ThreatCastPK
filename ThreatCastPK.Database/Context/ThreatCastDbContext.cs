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


            modelBuilder.Entity<Location>().HasData(
                new Location
                {
                    Id = Guid.Parse("3a3dce2d-0a91-4f9c-9c6d-4c0b0b3f2c01"),
                    CityName = "Karachi",
                    Province = "Sindh",
                    Latitude = 24.8607,
                    Longitude = 67.0011
                },
                new Location
                {
                    Id = Guid.Parse("9a1d4570-4b62-4a6d-86bb-5c9d5c5a9c02"),
                    CityName = "Lahore",
                    Province = "Punjab",
                    Latitude = 31.5204,
                    Longitude = 74.3587
                },
                new Location
                {
                    Id = Guid.Parse("b0c39e1f-ef6f-41b1-8d7b-3c5aa1f0e003"),
                    CityName = "Islamabad",
                    Province = "Islamabad Capital Territory",
                    Latitude = 33.6844,
                    Longitude = 73.0479
                },
                new Location
                {
                    Id = Guid.Parse("d2a2c9a4-5a0a-4b82-a6e5-2f1b8c1c5004"),
                    CityName = "Rawalpindi",
                    Province = "Punjab",
                    Latitude = 33.5651,
                    Longitude = 73.0169
                },
                new Location
                {
                    Id = Guid.Parse("e8b6c5f4-1c9a-4c12-9d6a-2b8f25d6e005"),
                    CityName = "Faisalabad",
                    Province = "Punjab",
                    Latitude = 31.4504,
                    Longitude = 73.1350
                },
                new Location
                {
                    Id = Guid.Parse("7f78f3a4-58a6-4cf1-8f2b-9e0f2c3a6006"),
                    CityName = "Multan",
                    Province = "Punjab",
                    Latitude = 30.1575,
                    Longitude = 71.5249
                },
                new Location
                {
                    Id = Guid.Parse("2c72a6e8-1a6e-4ec8-8c3f-1a8b5c7a7007"),
                    CityName = "Peshawar",
                    Province = "Khyber Pakhtunkhwa",
                    Latitude = 34.0151,
                    Longitude = 71.5805
                },
                new Location
                {
                    Id = Guid.Parse("51c8a5d6-65d2-44d6-a1e9-5f3d2a8b8008"),
                    CityName = "Quetta",
                    Province = "Balochistan",
                    Latitude = 30.1798,
                    Longitude = 66.9750
                },
                new Location
                {
                    Id = Guid.Parse("f4a86b77-0d9f-4c76-9f2f-9b7e8b6c9009"),
                    CityName = "Hyderabad",
                    Province = "Sindh",
                    Latitude = 25.3960,
                    Longitude = 68.3578
                },
                new Location
                {
                    Id = Guid.Parse("c9a7e2b1-6e3a-4e69-8b2f-6d6c7a7f0010"),
                    CityName = "Gujranwala",
                    Province = "Punjab",
                    Latitude = 32.1877,
                    Longitude = 74.1945
                },
                new Location
                {
                    Id = Guid.Parse("6d1c9f83-4c4c-4f8a-8ad9-1c2d3e4f1011"),
                    CityName = "Sialkot",
                    Province = "Punjab",
                    Latitude = 32.4945,
                    Longitude = 74.5229
                },
                new Location
                {
                    Id = Guid.Parse("ab93c7b8-1f2c-4a66-9e2a-7c8d9e0f1212"),
                    CityName = "Abbottabad",
                    Province = "Khyber Pakhtunkhwa",
                    Latitude = 34.1463,
                    Longitude = 73.2117
                }
            );

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

                entity.HasKey(p => p.Id);

                entity.HasOne(p => p.User)
                      .WithMany(u => u.DiscussionPosts)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.ParentPost)
                      .WithMany()
                      .HasForeignKey(p => p.ParentPostId)
                      .IsRequired(false)

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