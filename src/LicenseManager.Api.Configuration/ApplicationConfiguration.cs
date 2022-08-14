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

using LicenseManager.Api.Data.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace LicenseManager.Api.Configuration
{
    public class ApplicationConfiguration
    {
        /// <summary>
        /// The settings for test deployments.
        /// </summary>
        public TestingConfiguration Testing { get; set; } = new TestingConfiguration();

        /// <summary>
        /// The database connection strings and settings.
        /// </summary>
        public ConnectionStringsConfiguration ConnectionStrings { get; set; } = new ConnectionStringsConfiguration();

        /// <summary>
        /// The settings for the database provider.
        /// </summary>
        public DatabaseProviderConfiguration DatabaseProvider { get; set; } = new DatabaseProviderConfiguration();

        /// <summary>
        /// The settings for database migrations.
        /// </summary>
        public DatabaseMigrationsConfiguration DatabaseMigrations { get; set; } = new DatabaseMigrationsConfiguration();

        /// <summary>
        /// The settings for data protection.
        /// </summary>
        public DatabaseProtectionConfiguration DataProtection { get; set; } = new DatabaseProtectionConfiguration();

        /// <summary>
        /// The settings for Azure key vault.
        /// </summary>
        public AzureKeyVaultConfiguration AzureKeyVault { get; set; } = new AzureKeyVaultConfiguration();

        /// <summary>
        /// The identity service for user authentication.
        /// </summary>
        public IdentityConfiguration Identity { get; set; } = new IdentityConfiguration();

        /// <summary>
        /// The instance configuration.
        /// </summary>
        public InstanceConfiguration Instance { get; set; } = new InstanceConfiguration();

        /// <summary>
        /// Applies configuration parsed from an appsettings file into these options.
        /// </summary>
        /// <param name="configuration">The configuration to bind into this instance.</param>
        public ApplicationConfiguration(IConfiguration configuration)
        {
            configuration.GetSection(nameof(TestingConfiguration)).Bind(Testing);
            configuration.GetSection("ConnectionStrings").Bind(ConnectionStrings);
            configuration.GetSection(nameof(DatabaseProviderConfiguration)).Bind(DatabaseProvider);
            configuration.GetSection(nameof(DatabaseMigrationsConfiguration)).Bind(DatabaseMigrations);
            configuration.GetSection(nameof(DatabaseProtectionConfiguration)).Bind(DataProtection);
            configuration.GetSection(nameof(AzureKeyVaultConfiguration)).Bind(AzureKeyVault);
            configuration.GetSection(nameof(IdentityConfiguration)).Bind(Identity);
            configuration.GetSection(nameof(InstanceConfiguration)).Bind(Instance);
        }
    }
}