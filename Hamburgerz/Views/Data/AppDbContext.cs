using Hamburgerz.Models;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<RiskData> RiskData { get; set; }

        public DbSet<MeasurementAnswer> MeasurementAnswers { get; set; }

        public DbSet<AnalysisCache> AnalysisCache { get; set; }

        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
            });

            modelBuilder.Entity<Country>(entity =>
            {
                entity.HasIndex(c => c.Name).IsUnique();
            });

            modelBuilder.Entity<RiskData>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<MeasurementAnswer>()
                .HasOne<RiskData>()
                .WithMany()
                .HasForeignKey(a => a.RiskDataId);

            modelBuilder.Entity<MeasurementAnswer>()
                .HasIndex(a => new { a.UserId, a.QuestionKey, a.AnsweredAt });

            modelBuilder.Entity<AnalysisCache>()
                .HasIndex(c => c.UserId)
                .IsUnique();
        }
    }
}
