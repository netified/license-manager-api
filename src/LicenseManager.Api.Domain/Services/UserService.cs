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
using LicenseManager.Api.Configuration;
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Exceptions;
using LicenseManager.Api.Domain.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services;

public class UserService
{
    #region Service

    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly DataStoreDbContext _dataStore;
    private readonly ISieveProcessor _sieveProcessor;
    private readonly ILogger<UserService> _logger;
    private readonly IDistributedCache _cache;

    public UserService(IHttpContextAccessor contextAccessor, ApplicationConfiguration applicationConfiguration, DataStoreDbContext dataStore, ISieveProcessor sieveProcessor, ILogger<UserService> logger, IDistributedCache cache)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _applicationConfiguration = applicationConfiguration ?? throw new ArgumentNullException(nameof(applicationConfiguration));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    #endregion Service

    #region User Management

    /// <summary>
    /// Retrieve the users from the data store using the pagination functionality.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PagedResult<UserEntity>> ListAsync(SieveModel request, CancellationToken cancellationToken)
    {
        var query = _dataStore.Set<UserEntity>()
            .AsNoTracking().AsQueryable();
        return await _sieveProcessor.GetPagedAsync(query, request, cancellationToken);
    }

    /// <summary>
    /// Register current user to the product.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task<Guid> RegisterAsync(CancellationToken cancellationToken)
    {
        // Create new user in data store
        UserEntity userIdentity;
        try
        {
            userIdentity = new UserEntity()
            {
                RemoteId = _contextAccessor.GetHttpIdentifier(_applicationConfiguration.Identity),
                DisplayName = _contextAccessor.GetHttpDisplayName(_applicationConfiguration.Identity),
                UserName = _contextAccessor.GetHttpUserName(_applicationConfiguration.Identity),
                Email = _contextAccessor.GetHttpEmail(_applicationConfiguration.Identity)
            };
            await _dataStore.Set<UserEntity>().AddAsync(userIdentity, cancellationToken);
            await _dataStore.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to create user entity", ex);
            throw new Exception("Unable to create user entity");
        }

        // Create self organization in data store
        OrganizationEntity organizationEntity;
        try
        {
            organizationEntity = new OrganizationEntity()
            {
                Name = $"Personal#{userIdentity.Id}",
                Type = OrganizationType.Personal,
                Description = "Personal organization",
                UserOrganizations = new List<UserOrganizationEntity>()
            {
                new UserOrganizationEntity()
                {
                    UserId = userIdentity.Id,
                    Role = OrganizationRole.Owner
                }
            }
            };
            await _dataStore.Set<OrganizationEntity>().AddAsync(organizationEntity, cancellationToken);
            await _dataStore.SaveChangesAsync(userIdentity.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            // Delete the previously created account
            _dataStore.Set<UserEntity>().Remove(userIdentity);
            await _dataStore.SaveChangesAsync(cancellationToken);

            _logger.LogError("Unable to create self user organization", ex);
            throw new Exception("Unable to create self user organization");
        }

        // Set default organization
        userIdentity.DefaultOrganization = organizationEntity.Id;
        _dataStore.Set<UserEntity>().Update(userIdentity);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return userIdentity.Id;
    }

    /// <summary>
    /// Gets the effective user identifier.
    /// </summary>
    /// <returns>The effective user identifier</returns>
    public async Task<Guid> GetIdentifierAsync(CancellationToken cancellationToken)
    {
        var remoteIdentifier = _contextAccessor.GetHttpIdentifier(_applicationConfiguration.Identity);
        var cacheKey = $"user-{remoteIdentifier}";
        var cacheValue = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cacheValue == null)
        {
            var identifier = await GetSystemUserAsync(remoteIdentifier, cancellationToken);
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(identifier.ToString()), options, cancellationToken);
            return identifier;
        }
        else
        {
            // Convert and return user identifier
            var stringIdentifier = Encoding.UTF8.GetString(cacheValue);
            return Guid.Parse(stringIdentifier);
        }
    }

    /// <summary>
    /// Gets the system user identifier from the data store.
    /// </summary>
    /// <param name="remoteIdentifier">The remote identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task<Guid> GetSystemUserAsync(string remoteIdentifier, CancellationToken cancellationToken)
    {
        var userIdentifier = await _dataStore.Set<UserEntity>()
            .AsNoTracking()
            .Where(x => x.RemoteId == remoteIdentifier)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userIdentifier != Guid.Empty)
            return userIdentifier;
        return await RegisterAsync(cancellationToken);
    }

    public async Task<UserEntity> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = await GetIdentifierAsync(cancellationToken);
        var entity = await _dataStore.Set<UserEntity>()
            .AsNoTracking()
            .Include(x => x.License)
            .Where(x => x.Id == userId)
            .FirstAsync(cancellationToken);
        return entity;
    }

    public async Task SetDefaultOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var userId = await GetIdentifierAsync(cancellationToken);
        var entity = await _dataStore.Set<UserEntity>()
            .Include(x => x.UserOrganizations)
            .Where(x => x.Id == userId)
            .FirstAsync(cancellationToken);

        if (!entity.UserOrganizations.Any(x => x.OrganizationId == organizationId))
            throw new BadRequestExecption();

        entity.DefaultOrganization = organizationId;
        _dataStore.Set<UserEntity>().Update(entity);
        await _dataStore.SaveChangesAsync(cancellationToken);
    }

    #endregion User Management

    #region License management

    public async Task GetLicenseAsync(CancellationToken cancellationToken)
    {
        var userId = await GetIdentifierAsync(cancellationToken);
        await GetLicenseAsync(userId, cancellationToken);
    }

    public async Task GetLicenseAsync(Guid userId, CancellationToken cancellationToken)
    {
    }

    public async Task AddLicenseAsync(string license, CancellationToken cancellationToken)
    {
        var userId = await GetIdentifierAsync(cancellationToken);
        await AddLicenseAsync(userId, license, cancellationToken);
    }

    public async Task AddLicenseAsync(Guid userId, string license, CancellationToken cancellationToken)
    {
    }

    public async Task RemoveLicenseAsync(CancellationToken cancellationToken)
    {
        var userId = await GetIdentifierAsync(cancellationToken);
        await RemoveLicenseAsync(userId, cancellationToken);
    }

    public async Task RemoveLicenseAsync(Guid userId, CancellationToken cancellationToken)
    {
    }

    #endregion License management
}