using DefneAI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Db
{
    public class ModelDbContext : DbContext
    {
        public ModelDbContext(DbContextOptions options) : base(options)
        {
            IsDatabaseConfigured = options.Extensions.Any(
                extension => extension.Info.IsDatabaseProvider);
        }

        protected ModelDbContext()
        {
        }

        public bool IsDatabaseConfigured { get; }
        public DbSet<AIModelProvider> aIModelProviders { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Prompt> Prompts { get; set; }
        public DbSet<AIResponse> AIResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(chat => chat.Id);
                entity.HasIndex(chat => chat.CreatedAtUtc);
            });

            modelBuilder.Entity<Prompt>(entity =>
            {
                entity.HasKey(prompt => prompt.Id);
                entity.Property(prompt => prompt.Content).IsRequired();
                entity.HasIndex(prompt => new { prompt.ChatId, prompt.CreatedAtUtc });
                entity.HasOne(prompt => prompt.Chat)
                    .WithMany(chat => chat.Prompts)
                    .HasForeignKey(prompt => prompt.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AIResponse>(entity =>
            {
                entity.HasKey(response => response.Id);
                entity.Property(response => response.Content).IsRequired();
                entity.Property(response => response.ModelName)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.HasIndex(response => new { response.ChatId, response.CreatedAtUtc });
                entity.HasIndex(response => new { response.PromptId, response.CreatedAtUtc });
                entity.HasOne(response => response.Chat)
                    .WithMany(chat => chat.Responses)
                    .HasForeignKey(response => response.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(response => response.Prompt)
                    .WithMany(prompt => prompt.Responses)
                    .HasForeignKey(response => response.PromptId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
