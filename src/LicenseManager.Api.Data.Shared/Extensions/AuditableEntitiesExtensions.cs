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
        /// Populate special properties for all Auditable Entities in context.
        /// </summary>
        public static void UpdateAuditableEntities(this DbContext context, Guid editorUserId)
        {
            DateTime utcNow = DateTime.UtcNow;

            var changedEntries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added
                         || e.State == EntityState.Modified
                         || e.State == EntityState.Deleted);

            foreach (var dbEntry in changedEntries)
            {
                UpdateAuditableEntity(dbEntry, utcNow, editorUserId);
            }
        }

        private static void UpdateAuditableEntity(
            EntityEntry dbEntry, DateTime utcNow, Guid editorUserId)
        {
            object entity = dbEntry.Entity;

            switch (dbEntry.State)
            {
                case EntityState.Added:

                    if (entity is ICreationAuditable creationAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        creationAuditable.CreatedBy = editorUserId;
                    }
                    break;

                case EntityState.Modified:
                    if (entity is IModificationAuditable modificationAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);
                        modificationAuditable.UpdatedDataBy = editorUserId;
                        dbEntry.CurrentValues[nameof(IModificationAuditable.UpdatedDataBy)] = editorUserId;

                        if (entity is ICreationAuditable)
                        {
                            PreventPropertyOverwrite<string>(dbEntry, nameof(ICreationAuditable.CreatedBy));
                        }
                    }
                    break;

                case EntityState.Deleted:
                    if (entity is IDeletionAuditable deletionAuditable)
                    {
                        UpdateTrackableEntity(dbEntry, utcNow);

                        // change CurrentValues after dbEntry.State becomes EntityState.Unchanged
                        deletionAuditable.DeletedDataBy = editorUserId;
                        dbEntry.CurrentValues[nameof(IDeletionAuditable.DeletedDataBy)] = editorUserId;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}