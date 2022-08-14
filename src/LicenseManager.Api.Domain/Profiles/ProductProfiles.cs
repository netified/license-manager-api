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
    /// Product configuration for maps.
    /// </summary>
    public class ProductProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductProfile"/> class.
        /// </summary>
        public ProductProfile()
        {
            CreateMap<PagedResult<ProductEntity>, PagedResult<ProductDto>>();
            CreateMap<ProductEntity, ProductDto>()
                .ForMember(dest => dest.LicenseCount, o => o.MapFrom(src => src.Licenses.Count));
        }
    }

    /// <summary>
    /// Product request configuration for maps.
    /// </summary>
    public class ProductRequestProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRequestProfile"/> class.
        /// </summary>
        public ProductRequestProfile()
        {
            CreateMap<ProductRequest, ProductEntity>();
        }
    }

    /// <summary>
    /// Product request validator.
    /// </summary>
    public class ProductRequestValidator : AbstractValidator<ProductRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRequestValidator"/> class.
        /// </summary>
        public ProductRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Name).Length(4, 64);

            RuleFor(x => x.Description).Length(0, 128);

            RuleFor(x => x.Company).Length(4, 64);
        }
    }

    /// <summary>
    /// Product backup configuration for maps.
    /// </summary>
    public class ProductBackupProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductBackupProfile"/> class.
        /// </summary>
        public ProductBackupProfile()
        {
            CreateMap<ProductEntity, ProductBackupDto>()
                .ForMember(dest => dest.PassPhrase, o => o.Ignore())
                .ForMember(dest => dest.PrivateKey, o => o.Ignore())
                .ForMember(dest => dest.PublicKey, o => o.Ignore())
                .ReverseMap();
        }
    }
}