// Copyright (c) 2022 Netified <contact@netified.io>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using LicenseManager.Api.Configuration;
using LicenseManager.Api.Data.Configuration.PostgreSQL;
using LicenseManager.Api.Data.Configuration.SqlServer;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using PostgreSQLMigrationAssembly = LicenseManager.Api.Data.PostgreSQL.Helpers.MigrationAssembly;
using SqlMigrationAssembly = LicenseManager.Api.Data.SQLServer.Helpers.MigrationAssembly;

namespace LicenseManager.Api.Service.Helpers
{
    /// <summary>
    /// Database helper.
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Register DbContexts for this application.
        /// Configure the connection strings in AppSettings.json
        /// </summary>
        /// <typeparam name="TDataStoreDbContext"></typeparam>
        /// <typeparam name="TDataProtectionDbContext"></typeparam>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void RegisterDbContexts<TDataStoreDbContext, TDataProtectionDbContext>(this IServiceCollection services, ApplicationConfiguration configuration)
            where TDataStoreDbContext : DbContext
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            if (configuration.Testing.IsStaging)
                services.RegisterDbContextsStaging<TDataStoreDbContext, TDataProtectionDbContext>();
            else
                services.RegisterDbContextsProduction<TDataStoreDbContext, TDataProtectionDbContext>(configuration);
        }

        /// <summary>
        /// Registers the database contexts for staging.
        /// </summary>
        /// <typeparam name="TDataStoreDbContext">The type of the data store database context.</typeparam>
        /// <typeparam name="TDataProtectionDbContext">The type of the data protection database context.</typeparam>
        /// <param name="services">The services.</param>
        private static void RegisterDbContextsStaging<TDataStoreDbContext, TDataProtectionDbContext>(this IServiceCollection services)
            where TDataStoreDbContext : DbContext
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            var dataStoreDatabaseName = Guid.NewGuid().ToString();
            var dataProtectionDatabaseName = Guid.NewGuid().ToString();

            services.AddDbContext<TDataStoreDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseInMemoryDatabase(dataStoreDatabaseName);
                optionsBuilder.EnableSensitiveDataLogging();
            });
            services.AddDbContext<TDataProtectionDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseInMemoryDatabase(dataProtectionDatabaseName);
                optionsBuilder.EnableSensitiveDataLogging();
            });
        }

        /// <summary>
        /// Registers the database contexts for production.
        /// </summary>
        /// <typeparam name="TDataStoreDbContext">The type of the data store database context.</typeparam>
        /// <typeparam name="TDataProtectionDbContext">The type of the data protection database context.</typeparam>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        private static void RegisterDbContextsProduction<TDataStoreDbContext, TDataProtectionDbContext>(this IServiceCollection services, ApplicationConfiguration configuration)
            where TDataStoreDbContext : DbContext
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            // Set Migration assembly
            var migrationsAssembly = GetMigrationAssemblyByProvider(configuration.DatabaseProvider);
            configuration.DatabaseMigrations.SetMigrationsAssemblies(migrationsAssembly);

            // Register the database provider.
            switch (configuration.DatabaseProvider.ProviderType)
            {
                case DatabaseProviderType.SqlServer:
                    services.RegisterSqlServerDbContexts<TDataStoreDbContext, TDataProtectionDbContext>(configuration.ConnectionStrings, configuration.DatabaseMigrations);
                    break;

                case DatabaseProviderType.PostgreSQL:
                    services.RegisterNpgSqlDbContexts<TDataStoreDbContext, TDataProtectionDbContext>(configuration.ConnectionStrings, configuration.DatabaseMigrations);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(configuration.DatabaseProvider.ProviderType), $@"The value needs to be one of {string.Join(", ", Enum.GetNames(typeof(DatabaseProviderType)))}.");
            }
        }

        /// <summary>
        /// Gets the migration assembly by provider.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <returns></returns>
        private static string GetMigrationAssemblyByProvider(DatabaseProviderConfiguration databaseProvider)
        {
            return databaseProvider.ProviderType switch
            {
                DatabaseProviderType.SqlServer => typeof(SqlMigrationAssembly).GetTypeInfo().Assembly.GetName().Name,
                DatabaseProviderType.PostgreSQL => typeof(PostgreSQLMigrationAssembly).GetTypeInfo().Assembly.GetName().Name,
                _ => throw new ArgumentOutOfRangeException(nameof(databaseProvider.ProviderType), $@"The value needs to be one of {string.Join(", ", Enum.GetNames(typeof(DatabaseProviderType)))}.")
            };
        }
    }
}