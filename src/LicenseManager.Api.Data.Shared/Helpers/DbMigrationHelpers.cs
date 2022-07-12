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
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseManager.Api.Data.Shared.Helpers
{
    /// <summary>
    /// Helper to easily migrate the database.
    /// </summary>
    public static class DbMigrationHelpers
    {
        /// <summary>
        /// Apply all database migrations.
        /// </summary>
        /// <typeparam name="TDataStoreDbContext"></typeparam>
        /// <typeparam name="TDataProtectionDbContext"></typeparam>
        /// <param name="host"></param>
        /// <param name="databaseMigrationsConfiguration"></param>
        /// <returns></returns>
        public static async Task<bool> ApplyDbMigrationsAsync<TDataStoreDbContext, TDataProtectionDbContext>(IHost host, DatabaseMigrationsConfiguration databaseMigrationsConfiguration)
            where TDataStoreDbContext : DbContext
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            var migrationComplete = false;
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                if (databaseMigrationsConfiguration?.ApplyDatabaseMigrations == true)
                {
                    migrationComplete = await EnsureDatabasesMigratedAsync<TDataStoreDbContext, TDataProtectionDbContext>(services);
                }
            }
            return migrationComplete;
        }

        /// <summary>
        /// Ensure sure all databases are migrated.
        /// </summary>
        /// <typeparam name="TDataStoreDbContext"></typeparam>
        /// <typeparam name="TDataProtectionDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static async Task<bool> EnsureDatabasesMigratedAsync<TDataStoreDbContext, TDataProtectionDbContext>(IServiceProvider services)
            where TDataStoreDbContext : DbContext
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            int pendingMigrationCount = 0;
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<TDataStoreDbContext>())
                {
                    await context.Database.MigrateAsync();
                    pendingMigrationCount += (await context.Database.GetPendingMigrationsAsync()).Count();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<TDataProtectionDbContext>())
                {
                    await context.Database.MigrateAsync();
                    pendingMigrationCount += (await context.Database.GetPendingMigrationsAsync()).Count();
                }
            }

            return pendingMigrationCount == 0;
        }
    }
}