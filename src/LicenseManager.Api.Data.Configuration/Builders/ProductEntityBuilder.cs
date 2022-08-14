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
    public class ProductEntityBuilder : IEntityTypeConfiguration<ProductEntity>
    {
        public void Configure(EntityTypeBuilder<ProductEntity> builder)
        {
            builder.ToTable(name: "Products");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Name)
                .HasMaxLength(64)
                .IsRequired();
            builder.Property(x => x.Description)
                .HasMaxLength(128);
            builder.Property(x => x.PassPhrase)
                .IsRequired();
            builder.Property(x => x.PrivateKey)
                .IsRequired();
            builder.Property(x => x.PublicKey)
                .IsRequired();

            builder.Property(x => x.CreatedBy)
                .IsRequired();
            builder.Property(x => x.CreatedUtc)
                .IsRequired();

            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}