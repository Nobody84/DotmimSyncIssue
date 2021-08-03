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
            var resolveMode = MyResolveMode.ClientWinsTextOnly;

            // Create and Cleanup Sqlite database
            CreateAndCleanSqliteDatabse();

            // Start WebApi
            StartWebApi();

            // Create Sync WebClientOrchestrator and SqliteSyncProvider
            var serverOrchestrator = new WebClientOrchestrator(WebApiSyncUrl);
            var clientProvider = new SqliteSyncProvider(DbPathSqlite);

            // Create SyncAgent
            var options = new SyncOptions() { ConflictResolutionPolicy = ConflictResolutionPolicy.ServerWins };
            var syncAgent = new SyncAgent(clientProvider, serverOrchestrator, options);

            // From client : Remote is server, Local is client
            // From here, we are going to let the client decides
            // who is the winner of the conflict :
            syncAgent.LocalOrchestrator.OnApplyChangesFailed(acf =>
            {
                // Check conflict is correctly set
                var localRow = acf.Conflict.LocalRow;
                var remoteRow = acf.Conflict.RemoteRow;

                Console.WriteLine($"### LocalOrchestrator.OnApplyChangesFailed");
                Console.WriteLine($"Conflict: Type={acf.Conflict.Type}, ErrorMessage={acf.Conflict.ErrorMessage}");
                Console.WriteLine($"Local : Text={localRow["Text"]}");
                Console.WriteLine($"Remote: Text={remoteRow["Text"]}");
                Console.WriteLine();

                switch (resolveMode)
                {
                    case MyResolveMode.ClientWinsTextOnly:
                        // Client wins, only copy 'Text', ignore changes on 'LastModified'.
                        remoteRow["Text"] = localRow["Text"];
                        break;

                    case MyResolveMode.ServerWinsTextOnly:
                        // Server wins, only copy 'Text', ignore changes on 'LastModified'.
                        localRow["Text"] = remoteRow["Text"];
                        break;

                    case MyResolveMode.ClientWinsTextAndLastModified:
                        // Client wins, copy 'Text' and 'LastModified'.
                        localRow["Text"] = remoteRow["Text"];
                        localRow["LastModified"] = remoteRow["LastModified"];
                        break;
                }

                // Mandatory to override the winner registered in the tracking table
                // Use with caution !
                // To be sure the row will be marked as updated locally,
                // the scope id should be set to null
                acf.SenderScopeId = null;
            });

            // From Server : Remote is client, Local is server
            // From that point we do not do anything,
            // letting the server resolves the conflict and send back
            // the server row and client row conflicting to the client
            syncAgent.RemoteOrchestrator.OnApplyChangesFailed(acf =>
            {
                // Check conflict is correctly set
                var localRow = acf.Conflict.LocalRow;
                var remoteRow = acf.Conflict.RemoteRow;

                Console.WriteLine($"### RemoteOrchestrator.OnApplyChangesFailed");
                Console.WriteLine($"Conflict: Type={acf.Conflict.Type}, ErrorMessage={acf.Conflict.ErrorMessage}");
                Console.WriteLine($"Local : Text={localRow["Text"]}");
                Console.WriteLine($"Remote: Text={remoteRow["Text"]}");
            });


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

            /* 
             * Client wins
             * Use Text only.
             * Result: As expected.
             */
            Console.WriteLine("######################################################################");
            Console.WriteLine("### Iteration 0 - Client wins - Use Text only");
            resolveMode = MyResolveMode.ClientWinsTextOnly;

            // Change the "My new Item" on both databses
            await ChangeMyNewItem(resolveMode);

            // Print values of the database
            Console.WriteLine("### Value stored in the databases before sync.");
            PrintCurrenValues();

            // Sync using the ConflictResolutionPolicy.ClientWins as set in the option.
            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### First Sync", syncResult);

            Console.WriteLine("### Value stored in the databases after first sync.");
            PrintCurrenValues();

            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### Second Sync", syncResult);

            // Check values of the databases
            Console.WriteLine("### Value stored in the databases after second sync.");
            PrintCurrenValues();

            /* 
             * Server wins
             * Use Text only.
             * Result: As expected.
             */
            Console.WriteLine("######################################################################");
            Console.WriteLine("### Iteration 1 - Server wins - User Text only");
            resolveMode = MyResolveMode.ServerWinsTextOnly;

            // Change the "My new Item" on both databses
            await ChangeMyNewItem(resolveMode);

            // Print values of the database
            Console.WriteLine("### Value stored in the databases before sync.");
            PrintCurrenValues();

            // Sync using the ConflictResolutionPolicy.ClientWins as set in the option.
            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### First Sync", syncResult);

            Console.WriteLine("### Value stored in the databases after first sync.");
            PrintCurrenValues();

            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### Second Sync", syncResult);

            // Check values of the databases
            Console.WriteLine("### Value stored in the databases after second sync.");
            PrintCurrenValues();

            /* 
             * Client wins
             * Use Text and LastModified.
             * Result: As expected.
             */
            Console.WriteLine("######################################################################");
            Console.WriteLine("### Iteration 2 - Client wins - Use Text and LastModified");
            resolveMode = MyResolveMode.ClientWinsTextAndLastModified;

            // Change the "My new Item" on both databses
            await ChangeMyNewItem(resolveMode);

            // Print values of the database
            Console.WriteLine("### Value stored in the databases before sync.");
            PrintCurrenValues();

            // Sync using the ConflictResolutionPolicy.ClientWins as set in the option.
            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### First Sync", syncResult);

            Console.WriteLine("### Value stored in the databases after first sync.");
            PrintCurrenValues();

            syncResult = await syncAgent.SynchronizeAsync();
            PrintSyncResult("### Second Sync", syncResult);

            // Check values of the databases
            Console.WriteLine("### Value stored in the databases after second sync.");
            PrintCurrenValues();
        }

        private static void PrintSyncResult(string title, SyncResult syncResult)
        {
            Console.WriteLine(title);
            Console.WriteLine(syncResult);
            Console.WriteLine();
        }

        private static async Task ChangeMyNewItem(MyResolveMode resolveMode)
        {
            using (var dbContext = new MyDbContextSqlServer(ConnectionStringSqlServer))
            {
                dbContext.Items.First().Text = $"My new Item. Remote modfication. ResolveMode={resolveMode}";
                dbContext.Items.First().LastModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new MyDbContextSqlite($"Data Source={DbPathSqlite}"))
            {
                dbContext.Items.First().Text = $"My new Item. Local modfication. ResolveMode={resolveMode}";
                dbContext.Items.First().LastModified = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
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
                Console.WriteLine($"Sqlite   : Text={item.Text}, LastModified={item.LastModified.ToString("hh:mm:ss.FFF")}");
            }

            using (var dbContext = new MyDbContextSqlServer(ConnectionStringSqlServer))
            {
                var item = dbContext.Items.First();
                Console.WriteLine($"SqlServer: Text={item.Text}, LastModified={item.LastModified.ToString("hh:mm:ss.FFF")}");
            }
            Console.WriteLine();
        }
    }

    public enum MyResolveMode
    {
        ClientWinsTextOnly,
        ServerWinsTextOnly,
        ClientWinsTextAndLastModified,
    }
}
