using EnvironmentalSurvey.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvironmentalSurvey.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<CompetitionWinner> CompetitionWinners { get; set; }
        public DbSet<EffectiveParticipation> EffectiveParticipations { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<SupportInfo> SupportInfos { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.Role, e.RegistrationStatus });
            });

            // SurveyResponse unique constraint
            modelBuilder.Entity<SurveyResponse>(entity =>
            {
                entity.HasIndex(e => new { e.SurveyId, e.UserId }).IsUnique();
            });

            // CompetitionWinner unique constraint
            modelBuilder.Entity<CompetitionWinner>(entity =>
            {
                entity.HasIndex(e => new { e.CompetitionId, e.Rank }).IsUnique();
            });
        }
    }
}