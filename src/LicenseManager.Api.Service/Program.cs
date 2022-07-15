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
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Data.Shared.Helpers;
using Netboot.Utility.Logging.Extensions;
using Serilog;

namespace LicenseManager.Api.Service
{
    public class Program
    {
        /// <summary>
        /// The migrate only arguments
        /// </summary>
        private const string MigrateOnlyArgs = "/migrateonly";

        /// <summary>
        /// Main entry point to run this application.
        /// </summary>
        /// <param name="args"></param>
        public static async Task Main(string[] args)
        {
            var configuration = GetConfiguration(args).Build();

            // Initialize the logger with the product configuration
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                var host = CreateHostBuilder(args).Build();

                // Apply database migration if allowed
                var migrationComplete = await ApplyDbMigrationsAsync(configuration, host);
                if (args.Any(x => x == MigrateOnlyArgs))
                {
                    await host.StopAsync();
                    if (!migrationComplete)
                        Environment.ExitCode = -1;
                    return;
                }

                // Run application asynchronous
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Apply database migration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static async Task<bool> ApplyDbMigrationsAsync(IConfiguration configuration, IHost host)
        {
            var testingConfiguration = configuration.GetSection(nameof(TestingConfiguration)).Get<TestingConfiguration>();
            if (testingConfiguration?.IsStaging == false)
            {
                var databaseMigrationsConfiguration = configuration.GetSection(nameof(DatabaseMigrationsConfiguration))
                    .Get<DatabaseMigrationsConfiguration>();

                return await DbMigrationHelpers
                    .ApplyDbMigrationsAsync<DataStoreDbContext, DataProtectionDbContext>(host, databaseMigrationsConfiguration);
            }
            return true;
        }

        /// <summary>
        /// Create host builder.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            // Add serilog implementation
            builder.UseCustomSerilog();

            // Sets up the configuration for the remainder of the build process and application
            builder.ConfigureAppConfiguration((_, config) => config = GetConfiguration(args));

            // Configures a HostBuilder with defaults for hosting a web app.
            builder.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());

            return builder;
        }

        /// <summary>
        /// Build and return product configuration.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IConfigurationBuilder GetConfiguration(string[] args)
        {
            // Retrieve the name of the environment
            var aspnetcore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var dotnetcore = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var environment = string.IsNullOrWhiteSpace(aspnetcore) ? dotnetcore : aspnetcore;
            if (string.IsNullOrWhiteSpace(environment))
                environment = "Production";
            var isDevelopment = environment == Environments.Development;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"serilog.{environment}.json", optional: true, reloadOnChange: true);

            if (isDevelopment)
                configurationBuilder.AddUserSecrets<Startup>(true);

            configurationBuilder.AddCommandLine(args);
            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder;
        }
    }
}