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

namespace LicenseManager.Api.Domain.Profiles
{
    /// <summary>
    /// License configuration for maps.
    /// </summary>
    public class LicenseProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseProfile"/> class.
        /// </summary>
        public LicenseProfile()
        {
            CreateMap<LicenseEntity, LicenseDto>().ReverseMap();
            CreateMap<PagedResult<LicenseEntity>, PagedResult<LicenseDto>>();
        }
    }

    /// <summary>
    /// License request configuration for maps.
    /// </summary>
    public class LicenseRequestProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseRequestProfile"/> class.
        /// </summary>
        public LicenseRequestProfile()
        {
            CreateMap<LicenseRequest, LicenseEntity>()
                 .ForMember(dest => dest.ExpiresAt, o => o.MapFrom(src => DateTime.Now.AddDays(src.Duration)));
        }
    }

    /// <summary>
    /// License request validator.
    /// </summary>
    public class LicenseRequestValidator : AbstractValidator<LicenseRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseRequestValidator"/> class.
        /// </summary>
        public LicenseRequestValidator()
        {
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.Name).Length(4, 64);

            RuleFor(x => x.Email).NotNull();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Email).Length(4, 64);

            RuleFor(x => x.Duration).NotNull();
            RuleFor(x => x.Duration).InclusiveBetween(1, 1460);

            RuleFor(x => x.Type).NotNull();
            RuleFor(x => x.Type).Must(x => 
                x.Equals(LicenseType.Trial.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                x.Equals(LicenseType.Standard.ToString(), StringComparison.InvariantCultureIgnoreCase));
        }
    }

    /// <summary>
    /// License backup configuration for maps.
    /// </summary>
    public class LicenseBackupProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseBackupProfile"/> class.
        /// </summary>
        public LicenseBackupProfile()
        {
            CreateMap<LicenseEntity, LicenseBackupDto>().ReverseMap();
        }
    }
}