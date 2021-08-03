using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace DataEntities
{
    public class MyDbContextSqlite : MyDbContext
    {
        private static readonly ILoggerFactory ConsoleLogger = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public MyDbContextSqlite()
        {
        }

        public MyDbContextSqlite(string connectionString)
            : this(new DbContextOptionsBuilder<MyDbContextSqlite>()
                  //.UseLoggerFactory(ConsoleLogger)
                  .UseSqlite(connectionString).Options)
        {
        }

        public MyDbContextSqlite(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=:memory:");
                optionsBuilder.UseLoggerFactory(ConsoleLogger);
            }

            base.OnConfiguring(optionsBuilder);
        }
    }
}
