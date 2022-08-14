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

using LicenseManager.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManager.Api.Data.Configuration.Builders
{
    public class PermissionEntityBuilder : IEntityTypeConfiguration<PermissionEntity>
    {
        public void Configure(EntityTypeBuilder<PermissionEntity> builder)
        {
            builder.ToTable(name: "UserOrganizations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role)
                .IsRequired();

            builder.Property(x => x.CreatedBy)
                .IsRequired();
            builder.Property(x => x.CreatedUtc)
                .IsRequired();

            builder.HasOne(x => x.Tenant)
                .WithMany(x=>x.Permissions)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}