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

using Azure.Identity;
using LicenseManager.Api.Configuration;
using LicenseManager.Api.Data.Shared.Constants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.Api.Service.Extensions
{
    /// <summary>
    /// Data Protection Service Extensions
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Adds the data protection.
        /// </summary>
        /// <typeparam name="TDataProtectionDbContext">The type of the data protection database context.</typeparam>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddDataProtection<TDataProtectionDbContext>(this IServiceCollection services, AppConfiguration configuration)
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            AddDataProtection<TDataProtectionDbContext>(
                services,
                configuration.DataProtection,
                configuration.AzureKeyVault);
        }

        /// <summary>
        /// Adds the data protection.
        /// </summary>
        /// <typeparam name="TDataProtectionDbContext">The type of the data protection database context.</typeparam>
        /// <param name="services">The services.</param>
        /// <param name="dataProtectionConfiguration">The data protection configuration.</param>
        /// <param name="azureKeyVaultConfiguration">The azure key vault configuration.</param>
        private static void AddDataProtection<TDataProtectionDbContext>(this IServiceCollection services, DatabaseProtectionConfiguration dataProtectionConfiguration, AzureKeyVaultConfiguration azureKeyVaultConfiguration)
            where TDataProtectionDbContext : DbContext, IDataProtectionKeyContext
        {
            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName(DataProtectionConsts.ApplicationName)
                .PersistKeysToDbContext<TDataProtectionDbContext>();

            if (dataProtectionConfiguration.ProtectKeysWithAzureKeyVault)
            {
                if (azureKeyVaultConfiguration.UseClientCredentials)
                {
                    dataProtectionBuilder.ProtectKeysWithAzureKeyVault(
                        new Uri(azureKeyVaultConfiguration.DataProtectionKeyIdentifier),
                        new ClientSecretCredential(
                            azureKeyVaultConfiguration.TenantId,
                            azureKeyVaultConfiguration.ClientId,
                            azureKeyVaultConfiguration.ClientSecret));
                }
                else
                {
                    dataProtectionBuilder.ProtectKeysWithAzureKeyVault(new Uri(azureKeyVaultConfiguration.DataProtectionKeyIdentifier), new DefaultAzureCredential());
                }
            }
        }
    }
}