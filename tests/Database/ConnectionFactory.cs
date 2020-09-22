using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
namespace Cypretex.Data.Filters.Tests.Database
{
    public class ConnectionFactory : IDisposable
    {
        public static readonly ILoggerFactory MyLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });

        #region IDisposable Support  
        private bool disposedValue = false; // To detect redundant calls  

        public TestDbContext CreateContextForInMemory()
        {
            var option = new DbContextOptionsBuilder<TestDbContext>()
            //.UseLazyLoadingProxies()
            .UseLoggerFactory(MyLoggerFactory)
            // .EnableSensitiveDataLogging()
            .UseSqlite(@"Data Source=test.db").Options;


            var context = new TestDbContext(option);
            if (context != null)
            {
                //context.Database.EnsureDeleted();
                //context.Database.EnsureCreated();
            }

            return context;
        }

        public TestDbContext CreateContextForSQLite()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var option = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;

            var context = new TestDbContext(option);

            if (context != null)
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            return context;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}