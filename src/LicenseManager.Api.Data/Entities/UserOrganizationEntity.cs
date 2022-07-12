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
using LicenseManager.Api.Data.Enum;
using System;

namespace LicenseManager.Api.Data.Entities
{
    /// <summary>
    /// The user organization entity in the database.
    /// </summary>
    /// <seealso cref="ICreationAuditable" />
    public class UserOrganizationEntity : ICreationAuditable
    {
        #region Data

        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public OrganizationRole Role { get; set; }

        #endregion Data

        #region Navigation

        public OrganizationEntity Organization { get; set; }

        public UserEntity User { get; set; }

        #endregion Navigation

        #region Metadata

        public Guid CreatedBy { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }

        #endregion Metadata
    }
}