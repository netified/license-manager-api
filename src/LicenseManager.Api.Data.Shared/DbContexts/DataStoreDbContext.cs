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

using LicenseManager.Api.Data.Configuration.Builders;
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Data.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Data.Shared.DbContexts
{
    /// <summary>
    /// A database instance represents a session with the data store database.
    /// </summary>
    /// <seealso cref="DbContext" />
    public class DataStoreDbContext : DbContext
    {
        /// <summary>
        /// Gets the context accessor.
        /// </summary>
        /// <value>The context accessor.</value>
        protected IHttpContextAccessor ContextAccessor { get; }

        /// <summary>
        /// Gets or sets the licenses.
        /// </summary>
        /// <value>
        /// The licenses.
        /// </value>
        public DbSet<LicenseEntity> Licenses { get; set; }

        /// <summary>
        /// Gets or sets the products.
        /// </summary>
        /// <value>
        /// The products.
        /// </value>
        public DbSet<ProductEntity> Products { get; set; }

        /// <summary>
        /// Gets or sets the organizations.
        /// </summary>
        /// <value>
        /// The organizations.
        /// </value>
        public DbSet<OrganizationEntity> Organizations { get; set; }

        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        /// <value>
        /// The users.
        /// </value>
        public DbSet<UserEntity> Users { get; set; }

        /// <summary>
        /// Gets or sets the user organizations.
        /// </summary>
        /// <value>
        /// The user organizations.
        /// </value>
        public DbSet<UserOrganizationEntity> UserOrganizations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreDbContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="contextAccessor">The context accessor.</param>
        public DataStoreDbContext(DbContextOptions<DataStoreDbContext> options, IHttpContextAccessor contextAccessor)
           : base(options)
        {
            ContextAccessor = contextAccessor;
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new LicenseEntityBuilder());
            modelBuilder.ApplyConfiguration(new ProductEntityBuilder());
            modelBuilder.ApplyConfiguration(new OrganizationEntityBuilder());
            modelBuilder.ApplyConfiguration(new UserEntityBuilder());
            modelBuilder.ApplyConfiguration(new UserOrganizationEntityBuilder());
        }

        /// <summary>
        /// Saves all changes made in this context to the database
        /// </summary>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation. The task result contains the
        /// number of state entries written to the database.
        /// </returns>
        /// <remarks>
        /// See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information.
        /// </remarks>
        public Task<int> SaveChangesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            this.UpdateAuditableEntities(userId);
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}