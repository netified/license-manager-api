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

using AutoMapper;
using FluentValidation;
using LicenseManager.Api.Abstractions;
using LicenseManager.Api.Data.Entities;

namespace LicenseManager.Api.Domain.Models
{
    /// <summary>
    /// Tenant configuration for maps.
    /// </summary>
    public class TenantProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantProfile"/> class.
        /// </summary>
        public TenantProfile()
        {
            CreateMap<PagedResult<TenantEntity>, PagedResult<TenantDto>>();
            CreateMap<TenantEntity, TenantDto>()
                .ForMember(dest => dest.MemberCount, o => o.MapFrom(src => src.Permissions.Count));
        }
    }

    /// <summary>
    /// Tenant request configuration for maps.
    /// </summary>
    public class TenantRequestProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantRequestProfile"/> class.
        /// </summary>
        public TenantRequestProfile()
        {
            CreateMap<TenantRequest, TenantEntity>();
        }
    }

    /// <summary>
    /// Tenant request validator.
    /// </summary>
    public class TenantRequestValidator : AbstractValidator<TenantRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantRequestValidator"/> class.
        /// </summary>
        public TenantRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Name).Length(4, 64);
            RuleFor(x => x.Description).Length(0, 128);
        }
    }
}