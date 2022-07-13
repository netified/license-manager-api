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

using LicenseManager.Api.Abstractions;
using LicenseManager.Api.Data.Abstracts;
using System;
using System.Collections.Generic;

namespace LicenseManager.Api.Data.Entities
{
    /// <summary>
    /// The organization entity in the database.
    /// </summary>
    /// <seealso cref="ICreationAuditable" />
    public class OrganizationEntity : ICreationAuditable
    {
        #region Data

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public OrganizationType Type { get; set; }

        #endregion Data

        #region Navigation

        public ICollection<UserOrganizationEntity> UserOrganizations { get; set; }

        public ICollection<ProductEntity> Products { get; set; }

        #endregion Navigation

        #region Metadata

        public Guid CreatedBy { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }

        #endregion Metadata
    }
}