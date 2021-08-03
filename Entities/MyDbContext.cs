using Microsoft.EntityFrameworkCore;

namespace DataEntities
{
    public class MyDbContext : DbContext
    {
        public MyDbContext()
        {
        }

        public MyDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Entities.Item> Items { get; set; }
    }
}
