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
using LicenseManager.Api.Abstractions;
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Data.Shared.Constants;
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Exceptions;
using LicenseManager.Api.Domain.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services
{
    /// <summary>
    /// A license service.
    /// </summary>
    public class LicenseService
    {
        #region Service

        private readonly DataStoreDbContext _dataStore;
        private readonly ProductService _productService;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly IDataProtectionProvider _dataProtection;
        private readonly ILogger<ProductService> _logger;

        #endregion Service

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseService"/> class.
        /// </summary>
        /// <param name="dataStore">The data store.</param>
        /// <param name="productService">The product service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sieveProcessor">The sieve processor.</param>
        /// <param name="dataProtection">The data protection.</param>
        /// <param name="logger">The logger.</param>
        public LicenseService(DataStoreDbContext dataStore, ProductService productService, IMapper mapper, ISieveProcessor sieveProcessor, IDataProtectionProvider dataProtection, ILogger<ProductService> logger)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieve the product licenses from the data store using the pagination functionality.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<PagedResult<LicenseEntity>> ListAsync(Guid productId, SieveModel request, CancellationToken cancellationToken)
        {
            var productEntity = await _dataStore.Set<ProductEntity>()
                .AsNoTracking().Where(x => x.Id == productId).AnyAsync(cancellationToken);
            if (productEntity == default)
            {
                _logger.LogError("Unable to find the following product: {0}", productId);
                throw new NotFoundException($"Unable to find the following product: {productId}");
            }

            var query = _dataStore.Set<LicenseEntity>()
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .AsQueryable();
            return await _sieveProcessor.GetPagedAsync(query, request, cancellationToken);
        }

        /// <summary>
        /// Get product license by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<LicenseEntity> GetAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            var productEntity = await _dataStore.Set<ProductEntity>()
                .AsNoTracking().Where(x => x.Id == productId).AnyAsync(cancellationToken);
            if (productEntity == default)
            {
                _logger.LogError("Unable to find the following product: {0}", productId);
                throw new NotFoundException($"Unable to find the following product: {productId}");
            }

            var licenseEntity = await _dataStore.Set<LicenseEntity>()
                .Where(x => x.Id == licenseId)
                .FirstOrDefaultAsync(cancellationToken);
            if (licenseEntity == default)
            {
                _logger.LogError("Unable to find the following product license: {0}", licenseId);
                throw new NotFoundException($"Unable to find the following product license: {licenseId}");
            }

            return licenseEntity;
        }

        /// <summary>
        /// Delete product license by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task RemoveAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            var productEntity = await _dataStore.Set<ProductEntity>()
           .AsNoTracking().Where(x => x.Id == productId).AnyAsync(cancellationToken);
            if (productEntity == default)
            {
                _logger.LogError("Unable to find the following product: {0}", productId);
                throw new NotFoundException($"Unable to find the following product: {productId}");
            }

            var licenseEntity = await GetAsync(productId, licenseId, cancellationToken);
            _dataStore.Remove(licenseEntity);
            await _dataStore.SaveChangesAsync(cancellationToken);
        }

        public async Task<LicenseEntity> AddAsync(Guid productId, LicenseRequest request, CancellationToken cancellationToken)
        {
            var productEntity = await _dataStore.Set<ProductEntity>()
                .AsNoTracking().Where(x => x.Id == productId).AnyAsync(cancellationToken);
            if (productEntity == default)
            {
                _logger.LogError("Unable to find the following product: {0}", productId);
                throw new NotFoundException($"Unable to find the following product: {productId}");
            }

            var license = _mapper.Map<LicenseEntity>(request);
            license.ProductId = productId;
            await _dataStore.Set<LicenseEntity>().AddAsync(license, cancellationToken);
            await _dataStore.SaveChangesAsync(cancellationToken);
            return license;
        }

        public async Task<License> GenerateAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            // Retrieve the elements
            var product = await _productService.GetAsync(productId, cancellationToken);
            var licenseEntity = await GetAsync(productId, licenseId, cancellationToken);

            // Unprotect private key of the product.
            var protector = _dataProtection.CreateProtector(DataProtectionConsts.DefaultPurpose);
            var passPhrase = protector.Unprotect(product.PassPhrase);
            var privateKey = protector.Unprotect(product.PrivateKey);

            // Generate license data
            var licenseBuild = License.New();
            licenseBuild.WithUniqueIdentifier(licenseEntity.Id);
            licenseBuild.WithProduct(product.Id, product.Name);
            licenseBuild.As(licenseEntity.Type);
            licenseBuild.ExpiresAt(licenseEntity.ExpiresAt);
            if (licenseEntity.AdditionalAttributes?.Count > 0)
                licenseBuild.WithAdditionalAttributes(licenseEntity.AdditionalAttributes);
            if (licenseEntity.ProductFeatures?.Count > 0)
                licenseBuild.WithProductFeatures(licenseEntity.ProductFeatures);
            licenseBuild.LicensedTo(x =>
            {
                x.Email = licenseEntity.Email;
                x.Company = product.Company;
                x.Name = licenseEntity.Name;
            });

            return licenseBuild.CreateAndSignWithPrivateKey(privateKey, passPhrase);
        }

        public async Task<LicenseBackup> ExportAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            var licenseEntity = await GetAsync(productId, licenseId, cancellationToken);
            return _mapper.Map<LicenseBackup>(licenseEntity);
        }

        public async Task<LicenseEntity> ImportAsync(Guid productId, LicenseBackup backup, CancellationToken cancellationToken)
        {
            var licence = _mapper.Map<LicenseEntity>(backup);
            licence.ProductId = productId;
            await _dataStore.AddAsync(licence, cancellationToken);
            await _dataStore.SaveChangesAsync(cancellationToken);
            return licence;
        }
    }
}