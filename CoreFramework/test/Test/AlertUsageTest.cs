using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Alert;
using Core.Alert.Sqlite;
using Core.Modularity;
using Core.Modularity.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AlertUsageTest
    {
        [TestMethod]
        public async Task AddAlertEscalation_ShouldUseDefaultInMemoryStorage()
        {
            var services = new ServiceCollection();
            services.AddAlertEscalation();
            var serviceProvider = services.BuildServiceProvider();

            var storage = serviceProvider.GetRequiredService<IAlertStorageProvider>();
            var manager = serviceProvider.GetRequiredService<AlertEscalationManager>();
            var session = manager.GetOrCreate("capital-flow");

            var decision = await session.OnAnomalyAsync(
                new DateTime(2026, 6, 8),
                new DateTime(2026, 6, 8, 9, 30, 0),
                new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

            Assert.IsInstanceOfType<InMemoryAlertStorageProvider>(storage);
            Assert.IsTrue(decision.ShouldFire);
            Assert.AreEqual(1, decision.Sequence);
            Assert.AreEqual(2, decision.Total);
        }

        [TestMethod]
        public async Task AddAlertEscalation_WithSqlite_ShouldPersistStateAcrossContainers()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"core-alert-usage-{Guid.NewGuid():N}.db");
            var connectionString = $"Data Source={dbPath};Pooling=False";

            try
            {
                var firstProvider = BuildWithSqliteByExtension(connectionString);
                var firstManager = firstProvider.GetRequiredService<AlertEscalationManager>();
                var firstSession = firstManager.GetOrCreate("capital-flow");

                var firstDecision = await firstSession.OnAnomalyAsync(
                    new DateTime(2026, 6, 8),
                    new DateTime(2026, 6, 8, 9, 30, 0),
                    new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

                var secondProvider = BuildWithSqliteByExtension(connectionString);
                var secondStorage = secondProvider.GetRequiredService<IAlertStorageProvider>();
                var secondManager = secondProvider.GetRequiredService<AlertEscalationManager>();
                var secondSession = secondManager.GetOrCreate("capital-flow");

                var secondDecision = await secondSession.OnAnomalyAsync(
                    new DateTime(2026, 6, 8),
                    new DateTime(2026, 6, 8, 9, 40, 0),
                    new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

                var thirdDecision = await secondSession.OnAnomalyAsync(
                    new DateTime(2026, 6, 8),
                    new DateTime(2026, 6, 8, 9, 45, 0),
                    new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

                Assert.IsTrue(firstDecision.ShouldFire);
                Assert.IsInstanceOfType<SqliteAlertStorageProvider>(secondStorage);
                Assert.IsFalse(secondDecision.ShouldFire);
                Assert.IsTrue(thirdDecision.ShouldFire);
                Assert.AreEqual(2, thirdDecision.Sequence);
            }
            finally
            {
                DeleteIfExists(dbPath);
                DeleteIfExists(dbPath + "-wal");
                DeleteIfExists(dbPath + "-shm");
            }
        }

        public async Task ConfigureServiceCollection_WithModule_ShouldBindSqliteStorageFromConfiguration()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"core-alert-module-{Guid.NewGuid():N}.db");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Alert:Storage:ConnectionString"] = $"Data Source={dbPath};Pooling=False",
                })
                .Build();

            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configuration);
                services.ConfigureServiceCollection<AlertSqliteTestStartupModule>();
                var serviceProvider = services.BuildServiceProvider();

                var storage = serviceProvider.GetRequiredService<IAlertStorageProvider>();
                var manager = serviceProvider.GetRequiredService<AlertEscalationManager>();
                var session = manager.GetOrCreate("capital-flow");

                var firstDecision = await session.OnAnomalyAsync(
                    new DateTime(2026, 6, 8),
                    new DateTime(2026, 6, 8, 9, 30, 0),
                    new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

                var secondDecision = await session.OnAnomalyAsync(
                    new DateTime(2026, 6, 8),
                    new DateTime(2026, 6, 8, 9, 45, 0),
                    new[] { TimeSpan.Zero, TimeSpan.FromMinutes(15) });

                Assert.IsInstanceOfType<SqliteAlertStorageProvider>(storage);
                Assert.IsTrue(firstDecision.ShouldFire);
                Assert.IsTrue(secondDecision.ShouldFire);
                Assert.AreEqual(2, secondDecision.Sequence);
            }
            finally
            {
                DeleteIfExists(dbPath);
                DeleteIfExists(dbPath + "-wal");
                DeleteIfExists(dbPath + "-shm");
            }
        }

        private static ServiceProvider BuildWithSqliteByExtension(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddAlertEscalation(options =>
            {
                options.AddSqlite(sqlite =>
                {
                    sqlite.ConnectionString = connectionString;
                });
            });

            return services.BuildServiceProvider();
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        [DependsOn(typeof(CoreAlertSqliteModule))]
        private sealed class AlertSqliteTestStartupModule : CoreModuleBase
        {
        }
    }
}
