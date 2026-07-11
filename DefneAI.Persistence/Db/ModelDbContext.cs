using DefneAI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DefneAI.Persistence.Db
{
    public class ModelDbContext : DbContext
    {
        public ModelDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ModelDbContext()
        {
        }
        public DbSet<AIModelProvider> aIModelProviders { get; set; }

    }
}
