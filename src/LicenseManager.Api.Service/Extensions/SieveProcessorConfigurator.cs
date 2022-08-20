﻿// Copyright (c) 2022 Netified <contact@netified.io>
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
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace LicenseManager.Api.Service.Extensions;

public class SieveProcessorConfigurator : SieveProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SieveProcessorConfigurator"/> class.
    /// </summary>
    /// <param name="options"></param>
    public SieveProcessorConfigurator(IOptions<SieveOptions> options) : base(options)
    { }

    /// <summary>
    /// Configure the property mappers.
    /// </summary>
    /// <param name="mapper"></param>
    /// <returns></returns>
    protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
    {
        #region Product

        mapper.Property<ProductEntity>(p => p.Id).CanFilter();
        mapper.Property<ProductEntity>(p => p.Name).CanFilter().CanSort();
        mapper.Property<ProductEntity>(p => p.Description).CanFilter().CanSort();
        mapper.Property<ProductEntity>(p => p.Company).CanFilter().CanSort();
        mapper.Property<ProductEntity>(p => p.CreatedBy).CanFilter().CanSort();
        mapper.Property<ProductEntity>(p => p.CreatedUtc).CanFilter().CanSort();

        #endregion Product

        return mapper;
    }
}