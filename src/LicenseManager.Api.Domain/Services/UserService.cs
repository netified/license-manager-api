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
using LicenseManager.Api.Domain.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;

namespace LicenseManager.Api.Domain.Services;

public class UserService
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly AppConfiguration _appConfiguration;
    private readonly DataStoreDbContext _dataStore;
    private readonly ISieveProcessor _sieveProcessor;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="applicationConfiguration">The application configuration.</param>
    /// <param name="dataStore">The data store.</param>
    /// <param name="sieveProcessor">The sieve processor.</param>
    /// <param name="logger">The logger.</param>
    public UserService(
        IHttpContextAccessor contextAccessor,
        AppConfiguration applicationConfiguration,
        DataStoreDbContext dataStore,
        ISieveProcessor sieveProcessor,
        ILogger<UserService> logger)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _appConfiguration = applicationConfiguration ?? throw new ArgumentNullException(nameof(applicationConfiguration));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region User Management

    /// <summary>
    /// Retrieve users from the data store using the paging feature.
    /// </summary>
    /// <param name="filters">The filters.</param>
    /// <param name="sorts">The sorts.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">Size of the page.</param>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task<PagedResult<UserEntity>> ListAsync(string? filters, string? sorts, int? page, int? pageSize, CancellationToken stoppingToken = default)
    {
        var request = new SieveModel() { Filters = filters, Sorts = sorts, Page = page, PageSize = pageSize };
        var query = _dataStore.Set<UserEntity>().AsNoTracking();
        return await _sieveProcessor.GetPagedAsync(query, request, stoppingToken);
    }

    /// <summary>
    /// Retrieves the current user.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task<UserEntity> GetAsync(CancellationToken stoppingToken = default)
    {
        var remoteIdentifier = GetRemoteIdentifier();
        var userEntity = await _dataStore.Set<UserEntity>()
            .AsNoTracking()
            .Where(x => x.RemoteId == remoteIdentifier)
            .FirstOrDefaultAsync(stoppingToken);
        if (userEntity == default)
            userEntity = await RegisterAsync(stoppingToken);
        return userEntity;
    }

    /// <summary>
    /// Gets the current user's identifier.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task<Guid> GetIdentifierAsync(CancellationToken stoppingToken = default)
    {
        var userEntity = await GetAsync(stoppingToken);
        return userEntity.Id;
    }

    /// <summary>
    /// Register the current user to this application.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task<UserEntity> RegisterAsync(CancellationToken stoppingToken = default)
    {
        if (GetRemoteUserName().StartsWith("service-account"))
            return await RegisterServiceAccountAsync(stoppingToken);
        return await RegisterAccountAsync(stoppingToken);
    }

    /// <summary>
    /// Registers the service account.
    /// The service account contains minimal information to use this product.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    private async Task<UserEntity> RegisterServiceAccountAsync(CancellationToken stoppingToken = default)
    {
        var userEntity = new UserEntity()
        {
            RemoteId = GetRemoteIdentifier(),
            DisplayName = "Service Account",
            UserName = GetRemoteUserName(),
            Email = $"{GetRemoteUserName()}@netified.io".ToLower(),
            Prenium = _appConfiguration.Instance.Type == InstanceType.OnPremise
        };

        _dataStore.Set<UserEntity>().Add(userEntity);
        await _dataStore.SaveChangesAsync(stoppingToken);
        return userEntity;
    }

    /// <summary>
    /// Registers the standard account.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    private async Task<UserEntity> RegisterAccountAsync(CancellationToken stoppingToken = default)
    {
        var userEntity = new UserEntity()
        {
            RemoteId = GetRemoteIdentifier(),
            DisplayName = GetRemoteDisplayName(),
            UserName = GetRemoteUserName(),
            Email = GetRemoteEmail(),
            Prenium = _appConfiguration.Instance.Type == InstanceType.OnPremise
        };

        _dataStore.Set<UserEntity>().Add(userEntity);
        await _dataStore.SaveChangesAsync(stoppingToken);
        await InitializeTenantAsync(userEntity, stoppingToken);

        return userEntity;
    }

    /// <summary>
    /// Initializes the user's default tenant.
    /// </summary>
    /// <param name="userEntity">The user entity.</param>
    /// <param name="stoppingToken">The cancellation token.</param>
    private async Task InitializeTenantAsync(UserEntity userEntity, CancellationToken stoppingToken = default)
    {
        // Create default tenant
        var tenantId = userEntity.Id.GetHashCode().ToString().Replace("-", "");
        var tenantEntity = new TenantEntity()
        {
            Name = $"Personal #{tenantId}",
            Type = TenantType.Personal,
            Description = "Personal organization",
        };
        _dataStore.Set<TenantEntity>().Add(tenantEntity);
        await _dataStore.SaveChangesAsync(
            userId: userEntity.Id,
            cancellationToken: stoppingToken);

        // Add default permission
        var permissionEntity = new PermissionEntity()
        {
            UserId = userEntity.Id,
            TenantId = tenantEntity.Id,
            Role = UserRoleType.Owner
        };
        _dataStore.Set<PermissionEntity>().Add(permissionEntity);
        await _dataStore.SaveChangesAsync(
            userId: userEntity.Id,
            cancellationToken: stoppingToken);

        // Set default organization
        userEntity.DefaultTenant = tenantEntity.Id;
        _dataStore.Set<UserEntity>().Update(userEntity);
        await _dataStore.SaveChangesAsync(
            userId: userEntity.Id,
            cancellationToken: stoppingToken);
    }

    /// <summary>
    /// Sets the default tenant asynchronous.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task SetDefaultTenantAsync(Guid tenantId, CancellationToken stoppingToken)
    {
        var userId = await GetIdentifierAsync(stoppingToken);
        var entity = await _dataStore.Set<UserEntity>()
            .Where(x => x.Id == userId)
            .FirstAsync(stoppingToken);

        entity.DefaultTenant = tenantId;
        _dataStore.Set<UserEntity>().Update(entity);
        await _dataStore.SaveChangesAsync(stoppingToken);
    }

    #endregion User Management

    #region Remote data

    /// <summary>
    /// Gets the effective user identifier from the http context.
    /// </summary>
    /// <returns>The effective user identifier</returns>
    public string GetRemoteIdentifier()
    {
        if (string.IsNullOrEmpty(_appConfiguration.Identity.RemoteId))
        {
            throw new Exception("The remote identifier configuration null or empty, " +
                "please send the request with valid authentication");
        }

        var user = _contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
        {
            throw new Exception("Unable to find authentication context, " +
                "please send the request with valid authentication");
        }

        var stringIdentifier = user!.FindFirst(_appConfiguration.Identity.RemoteId)?.Value;
        if (string.IsNullOrEmpty(stringIdentifier))
        {
            throw new Exception("The name identifier is null or empty, " +
                "please send the request with valid authentication");
        }

        return stringIdentifier;
    }

    /// <summary>
    /// Gets the display name from the http context.
    /// </summary>
    /// <returns>The user mail</returns>
    public string GetRemoteDisplayName()
    {
        if (string.IsNullOrEmpty(_appConfiguration.Identity.DisplayName))
            throw new Exception("The display name configuration null or empty, " +
                "please send the request with valid authentication");

        var user = _contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new Exception("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(_appConfiguration.Identity.DisplayName)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new Exception("The display name is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    /// <summary>
    /// Gets the user name from the http context.
    /// </summary>
    /// <returns>The user name</returns>
    public string GetRemoteUserName()
    {
        if (string.IsNullOrEmpty(_appConfiguration.Identity.UserName))
            throw new Exception("The user name configuration null or empty, " +
                "please send the request with valid authentication");

        var user = _contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new Exception("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(_appConfiguration.Identity.UserName)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new Exception("The user name is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    /// <summary>
    /// Gets the email from the http context.
    /// </summary>
    /// <returns>The user email</returns>
    public string GetRemoteEmail()
    {
        if (string.IsNullOrEmpty(_appConfiguration.Identity.Email))
            throw new Exception("The user mail configuration null or empty, " +
                "please send the request with valid authentication");

        var user = _contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new Exception("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(_appConfiguration.Identity.Email)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new Exception("The email address is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    #endregion Remote data
}