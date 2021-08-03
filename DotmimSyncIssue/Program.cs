using DataEntities;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Sqlite;
using Dotmim.Sync.Web.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotmimSyncIssue
{
    class Program
    {
        private static string ConnectionStringSqlServer = "Server=sqlserver.domain.com,1433;Database=SyncIssue;User Id=SA;Password=secret;";
        private static string DbPathSqlite = "SyncIssue.db";
        private static string WebApiSyncUrl = "http://localhost:5000/api/sync";

        static async Task Main(string[] args)
        {
            // Create and Cleanup Sqlite database
            CreateAndCleanSqliteDatabse();

            // Start WebApi
            StartWebApi();

            // Create Sync WebClientOrchestrator and SqliteSyncProvider
            var serverOrchestrator = new WebClientOrchestrator(WebApiSyncUrl);
            var clientProvider = new SqliteSyncProvider(DbPathSqlite);

            // Create SyncAgent
            var options = new SyncOptions() { ConflictResolutionPolicy = ConflictResolutionPolicy.ClientWins };
            var syncAgent = new SyncAgent(clientProvider, serverOrchestrator, options);

            // Add Some date to the remote database and sync
            using (var dbContext = new MyDbContextSqlServer(ConnectionStringSqlServer))
            {
                // Clear the items table
                var allItems = await dbContext.Items.ToListAsync();
                dbContext.Items.RemoveRange(allItems);

                // Add new item row
                var newItem = new DataEntities.Entities.Item()
                {
                    Text = "My new Item.",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                dbContext.Items.Add(newItem);
                await dbContext.SaveChangesAsync();
            }

            // Initial sync to make sure both database are synced and have one entry "My new Item"
            var syncResult = await syncAgent.SynchronizeAsync();
            Console.WriteLine("### Initial Sync");
            Console.WriteLine(syncResult);
            Console.WriteLine();

            // Change the "My new Item" on both databses
            using (var dbContext = new MyDbContextSqlServer(ConnectionStringSqlServer))
            {
                dbContext.Items.First().Text = "My new Item. Remote modfication.";
                dbContext.Items.First().LastModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MyDbContextSqlite($"Data Source={DbPathSqlite}"))
            {
                dbContext.Items.First().Text = "My new Item. Local modfication.";
                dbContext.Items.First().LastModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            // Print values of the database
            Console.WriteLine("### Value stored in the databases before sync.");
            PrintCurrenValues();

            // Sync using the ConflictResolutionPolicy.ClientWins as set in the option.
            syncResult = await syncAgent.SynchronizeAsync();
            Console.WriteLine("### Second Sync");
            Console.WriteLine(syncResult);
            Console.WriteLine();

            // Check values of the databases
            Console.WriteLine("### Value stored in the databases after sync.");
            PrintCurrenValues();
        }

        private static void StartWebApi()
        {
            WebApi.Program.CreateHostBuilder(new string[] { }).Build().RunAsync();
        }

        private static void CreateAndCleanSqliteDatabse()
        {
            if (File.Exists(DbPathSqlite))
            {
                File.Delete(DbPathSqlite);
            }

            using (var dbContext = new MyDbContextSqlite($"Data Source={DbPathSqlite}"))
            {
                // Create SqlDatabase
                dbContext.Database.Migrate();
            }
        }

        private static void PrintCurrenValues()
        {
            using (var dbContext = new MyDbContextSqlite($"Data Source={DbPathSqlite}"))
            {
                var item = dbContext.Items.First();
                Console.WriteLine($"Sqlite   : Text={item.Text}, Created={item.Created.ToString("hh:mm:ss.FFF")}");
            }

            using (var dbContext = new MyDbContextSqlServer(ConnectionStringSqlServer))
            {
                var item = dbContext.Items.First();
                Console.WriteLine($"SqlServer: Text={item.Text}, Created={item.Created.ToString("hh:mm:ss.FFF")}");
            }
            Console.WriteLine();
        }
    }
}
