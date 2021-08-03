using DataEntities;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // [Required]: To be able to handle multiple sessions
            services.AddMemoryCache();

            // [Required]: Get a connection string to your server data source
            var connectionString = Configuration.GetConnectionString("SqlServer");

            // [Required]: Tables list involved in the sync process
            var tables = new string[] { "Items" };

            // [Required]: Add a SqlSyncProvider acting as the server hub.
            var options = new SyncOptions { ConflictResolutionPolicy = ConflictResolutionPolicy.ServerWins };
            services.AddSyncServer<SqlSyncChangeTrackingProvider>(connectionString, tables, options);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            using (var context = this.GetContext())
            {
                context.Database.Migrate();
            }
        }

        private MyDbContext GetContext()
        {
            var connectionString = Configuration.GetConnectionString("SqlServer");
            return new MyDbContextSqlServer(new DbContextOptionsBuilder().UseSqlServer($"{connectionString}").EnableDetailedErrors().Options);
        }
    }
}
