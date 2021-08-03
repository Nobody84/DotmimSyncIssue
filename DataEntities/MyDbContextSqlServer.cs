using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DataEntities
{
    public class MyDbContextSqlServer : MyDbContext
    {
        private static readonly ILoggerFactory ConsoleLogger = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public MyDbContextSqlServer()
        {
        }

        public MyDbContextSqlServer(string connectionString)
            : this(new DbContextOptionsBuilder<MyDbContextSqlServer>()
                  //.UseLoggerFactory(ConsoleLogger)
                  .UseSqlServer(connectionString).Options)
        {
        }

        public MyDbContextSqlServer(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder is null)
            {
                throw new System.ArgumentNullException(nameof(optionsBuilder));
            }

            if (!optionsBuilder.IsConfigured)
            {
                var migrationPath = Path.Combine(System.IO.Path.GetTempPath(), "migration.mdf");
                optionsBuilder.UseSqlServer($@"Server=.\SQLExpress;AttachDbFilename={migrationPath};Database=migration;Trusted_Connection=Yes;");
            }

            base.OnConfiguring(optionsBuilder);
        }
    }
}
