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

using LicenseManager.Api.Data.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;

namespace LicenseManager.Api.Data.Shared.Extensions
{
    internal static partial class DbContextExtensions
    {
        /// <summary>
        /// Populate special properties for all Trackable Entities in context.
        /// </summary>
        public static void UpdateTrackableEntities(this DbContext context)
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateTrackableEntity(dbEntry, utcNow);
            }
        }

        /// <summary>
        /// Populate special properties for single entity in context.
        /// </summary>
        /// <param name="dbEntry">The database entry.</param>
        /// <param name="utcNow">The UTC now.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        private static void UpdateTrackableEntity(EntityEntry dbEntry, DateTimeOffset utcNow)
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:
                    if (entity is ICreationTrackable creationTrackable)
                    {
                        creationTrackable.CreatedUtc = utcNow;
                    }
                    break;

                case EntityState.Modified:
                    if (entity is IModificationTrackable modificatonTrackable)
                    {
                        modificatonTrackable.UpdatedUtc = utcNow;
                        dbEntry.CurrentValues[nameof(IModificationTrackable.UpdatedUtc)] = utcNow;

                        if (entity is ICreationTrackable)
                        {
                            PreventPropertyOverwrite<DateTime>(dbEntry, nameof(ICreationTrackable.CreatedUtc));
                        }
                    }
                    break;

                case EntityState.Deleted:
                    if (entity is ISoftDeletable softDeletable)
                    {
                        dbEntry.State = EntityState.Unchanged;
                        softDeletable.IsDeletedData = true;
                        dbEntry.CurrentValues[nameof(ISoftDeletable.IsDeletedData)] = true;

                        if (entity is IDeletionTrackable deletionTrackable)
                        {
                            deletionTrackable.DeletedDataUtc = utcNow;
                            dbEntry.CurrentValues[nameof(IDeletionTrackable.DeletedDataUtc)] = utcNow;
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// If we set <see cref="EntityEntry.State"/> to <see cref="EntityState.Modified"/> on entity with
        /// empty <see cref="ICreationTrackable.CreatedUtc"/> or <see cref="ICreationAuditable.CreatorUserId"/>
        /// we should not overwrite database values.
        /// https://github.com/gnaeus/EntityFramework.CommonTools/issues/4
        /// </summary>
        private static void PreventPropertyOverwrite<TProperty>(EntityEntry dbEntry, string propertyName)
        {
            var propertyEntry = dbEntry.Property(propertyName);

            if (propertyEntry.IsModified && Equals(dbEntry.CurrentValues[propertyName], default(TProperty)))
            {
                propertyEntry.IsModified = false;
            }
        }
    }
}