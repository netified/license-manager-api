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
using LicenseManager.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services;

/// <summary>
/// A product service.
/// </summary>
public class ProductService
{
    #region Service

    private readonly DataStoreDbContext _dataStore;
    private readonly IdentityService _identityService;
    private readonly PermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly ISieveProcessor _sieveProcessor;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly ILogger<ProductService> _logger;

    #endregion Service

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    /// <param name="dataStore">The data store.</param>
    /// <param name="identityService">The identity service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="sieveProcessor">The sieve processor.</param>
    /// <param name="dataProtection">The data protection.</param>
    /// <param name="logger">The logger.</param>
    public ProductService(DataStoreDbContext dataStore, IdentityService identityService, PermissionService permissionService, IMapper mapper, ISieveProcessor sieveProcessor, IDataProtectionProvider dataProtection, ILogger<ProductService> logger)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieve the products from the data store using the pagination functionality.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PagedResult<ProductEntity>> ListAsync(SieveModel request, CancellationToken cancellationToken)
    {
        var userId = await _identityService.GetIdentifierAsync(cancellationToken);
        var query = _dataStore.Set<ProductEntity>()
            .AsNoTracking()
            .Include(x => x.Organization)
                .ThenInclude(x => x.UserOrganizations)
            .Include(x=>x.Licenses)
            .Where(x => x.Organization.UserOrganizations.Any(x => x.UserId == userId))
            .AsSplitQuery();
        return await _sieveProcessor.GetPagedAsync(query, request, cancellationToken);
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ConflitException"></exception>
    /// <returns></returns>
    public async Task<ProductEntity> AddAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CanManageOrganization(request.OrganizationId, cancellationToken))
            throw new UnauthorizedAccessException();

        // Detect if the request conflicts with the data store
        var detectConflit = await _dataStore.Set<ProductEntity>()
            .AnyAsync(x => x.Name == request.Name && x.OrganizationId == request.OrganizationId, cancellationToken);
        if (detectConflit)
            throw new ConflitException($"The following product name already exists: {request.Name}");

        // Map the product request to product
        var product = _mapper.Map<ProductEntity>(request);

        // Create a new public/private key pair for your product
        var passPhrase = GeneratePassPhrase();
        var keyGenerator = KeyGenerator.Create();
        var keyPair = keyGenerator.GenerateKeyPair();
        var privateKey = keyPair.ToEncryptedPrivateKeyString(passPhrase);
        var publicKey = keyPair.ToPublicKeyString();

        // Protect sensitive data
        var protector = _dataProtection.CreateProtector(DataProtectionConsts.DefaultPurpose);
        product.PassPhrase = protector.Protect(passPhrase);
        product.PrivateKey = protector.Protect(privateKey);
        product.PublicKey = protector.Protect(publicKey);

        // Save data to the data store
        var userId = await _identityService.GetIdentifierAsync(cancellationToken);
        await _dataStore.Set<ProductEntity>().AddAsync(product, cancellationToken);
        await _dataStore.SaveChangesAsync(userId, cancellationToken);
        _logger.LogInformation("The following product has been created: {productName}", product.Name);

        return product;
    }

    public Task<object> UpdateAsync(Guid productId, ProductRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieve the product from the data store.
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotFoundException"></exception>
    /// <returns></returns>
    public async Task<ProductEntity> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CanViewProduct(productId, cancellationToken))
            throw new UnauthorizedAccessException();

        // Retrieve the product from the data store
        var productEntity = await _dataStore.Set<ProductEntity>()
            .Where(x => x.Id == productId)
            .FirstOrDefaultAsync(cancellationToken);

        // Return the product if it exists
        return productEntity == default ?
            throw new NotFoundException(nameof(ProductEntity))
            : productEntity;
    }

    /// <summary>
    /// Delete the product from the data store.
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotFoundException"></exception>
    /// <returns></returns>
    public async Task RemoveAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CanManageProduct(productId, cancellationToken))
            throw new UnauthorizedAccessException();

        // Retrieve the product from the datastore
        var productEntity = await GetAsync(productId, cancellationToken);

        // Delete product in data store
        _dataStore.Set<ProductEntity>().Remove(productEntity);
        await _dataStore.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("The following product has been deleted: {productName}", productEntity.Name);
    }

    /// <summary>
    /// Generate new pass phrase to create a new public/private key pair.
    /// </summary>
    private static string GeneratePassPhrase()
    {
        using SHA256 sha256Hash = SHA256.Create();
        var bytes = new byte[sizeof(int)];
        return string.Concat(sha256Hash.ComputeHash(bytes).Select(x => x.ToString("X2")));
    }

    /// <summary>
    /// Export product configuration.
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotFoundException"></exception>
    /// <returns></returns>
    public async Task<ProductBackup> ExportAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CanManageProduct(productId, cancellationToken))
            throw new UnauthorizedAccessException();

        // Retrieve the product from the datastore
        var productEntity = await GetAsync(productId, cancellationToken);

        // Convert to backup object
        var productBackup = _mapper.Map<ProductBackup>(productEntity);

        // Unprotect the public/private key pair
        var protector = _dataProtection.CreateProtector(DataProtectionConsts.DefaultPurpose);
        productBackup.PassPhrase = protector.Unprotect(productEntity.PassPhrase);
        productBackup.PrivateKey = protector.Unprotect(productEntity.PrivateKey);
        productBackup.PublicKey = protector.Unprotect(productEntity.PublicKey);

        // Calculate Checksum
        var agregate = new StringBuilder();
        agregate.Append(productBackup.Id).Append(productBackup.OrganizationId).Append(productBackup.Name);
        agregate.Append(productBackup.Description).Append(productBackup.Company);
        agregate.Append(productBackup.PassPhrase).Append(productBackup.PrivateKey).Append(productBackup.PublicKey);
        productBackup.Checksum = agregate.ToString().ComputeMd5Hash();

        return productBackup;
    }

    /// <summary>
    /// Import product configuration.
    /// </summary>
    /// <param name="backup"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ConflitException"></exception>
    /// <exception cref="BadRequestExecption"></exception>
    public async Task<ProductEntity> ImportAsync(ProductBackup backup, bool checksumValidation, CancellationToken cancellationToken)
    {
        if (!await _permissionService.CanManageOrganization(backup.OrganizationId, cancellationToken))
            throw new UnauthorizedAccessException();

        // Calculate and validate the checksum
        if (checksumValidation)
        {
            var agregate = new StringBuilder();
            agregate.Append(backup.Id).Append(backup.OrganizationId).Append(backup.Name);
            agregate.Append(backup.Description).Append(backup.Company);
            agregate.Append(backup.PassPhrase).Append(backup.PrivateKey).Append(backup.PublicKey);
            var checksum = agregate.ToString().ComputeMd5Hash();
            if (backup.Checksum != checksum)
                throw new BadRequestExecption("Invalid checksum");
        }

        // Detect if the request conflicts with the data store
        var detectNameConflit = await _dataStore.Set<ProductEntity>().AnyAsync(x => x.Name == backup.Name, cancellationToken);
        if (detectNameConflit)
            throw new ConflitException($"The following product name already exists: {backup.Name}");
        var detectIdConflit = await _dataStore.Set<ProductEntity>().AnyAsync(x => x.Id == backup.Id, cancellationToken);
        if (detectIdConflit)
            throw new ConflitException($"The following product identifier already exists: {backup.Id}");

        // Map the product request to product
        var product = _mapper.Map<ProductEntity>(backup);

        // Protect sensitive data
        var protector = _dataProtection.CreateProtector(DataProtectionConsts.DefaultPurpose);
        product.PassPhrase = protector.Protect(backup.PassPhrase);
        product.PrivateKey = protector.Protect(backup.PrivateKey);
        product.PublicKey = protector.Protect(backup.PublicKey);

        // Validate the public/private key pair
        if (!ValidateKeyPair(backup.PrivateKey, backup.PassPhrase, backup.PublicKey))
            throw new BadRequestExecption("Invalid public/private key pair");

        // Save data to the data store
        await _dataStore.Set<ProductEntity>().AddAsync(product, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("The following product has been imported: {productName}", product.Name);

        return product;
    }

    /// <summary>
    /// Valid the public/private key pair.
    /// </summary>
    /// <param name="privateKey"></param>
    /// <param name="passPhrase"></param>
    /// <param name="publicKey"></param>
    /// <returns></returns>
    private bool ValidateKeyPair(string privateKey, string passPhrase, string publicKey)
    {
        try
        {
            var license = License.New().CreateAndSignWithPrivateKey(privateKey, passPhrase);
            return license.VerifySignature(publicKey);
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}