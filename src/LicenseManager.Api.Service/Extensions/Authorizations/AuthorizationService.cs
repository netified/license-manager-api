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
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace LicenseManager.Api.Service.Extensions.Authorizations
{
    /// <summary>
    /// The authorization service provides all authorization checks.
    /// </summary>
    public class AuthorizationService
    {
        private readonly DataStoreDbContext _dataStore;
        private readonly UserService _userService;
        private readonly IDistributedCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationService"/> class.
        /// </summary>
        /// <param name="dataStore">The data store.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="cache">The cache.</param>
        public AuthorizationService(DataStoreDbContext dataStore, UserService userService, IDistributedCache cache)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #region Authorization Component

        /// <summary>
        /// Determines whether is user have prenium license.
        /// </summary>
        public async Task<bool> IsPreniumAsync()
        {
            var remoteId = _userService.GetRemoteIdentifier();
            var cacheKey = $"{nameof(UserRoleType)}{remoteId}prenium";
            if (await _cache.GetAsync(cacheKey) == null)
            {
                var currentUser = await _userService.GetAsync();
                if (currentUser != null && currentUser.Prenium)
                {
                    await SetCacheAsync(cacheKey, "ok", TimeSpan.FromHours(1));
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether is member of the application.
        /// </summary>
        public async Task<bool> IsMemberAsync()
        {
            var remoteId = _userService.GetRemoteIdentifier();
            var cacheKey = $"{nameof(UserRoleType)}{remoteId}";
            if (await _cache.GetAsync(cacheKey) == null)
            {
                var currentUser = await _userService.GetAsync();
                if (currentUser != null)
                {
                    await SetCacheAsync(cacheKey, "ok", TimeSpan.FromHours(1));
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether is tenant reader.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        public async Task<bool> IsTenantReaderAsync(Guid tenantId)
        {
            var role = await GetTenantRoleAsync(tenantId);
            return role != UserRoleType.None;
        }

        /// <summary>
        /// Determines whether is tenant manager.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        public async Task<bool> IsTenantManagerAsync(Guid tenantId)
        {
            var role = await GetTenantRoleAsync(tenantId);
            return role == UserRoleType.Owner;
        }

        /// <summary>
        /// Determines whether is product reader.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        public async Task<bool> IsProductReaderAsync(Guid productId)
        {
            var role = await GetProductRoleAsync(productId);
            return role != UserRoleType.None;
        }

        /// <summary>
        /// Determines whether is product manager.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        public async Task<bool> IsProductManagerAsync(Guid productId)
        {
            var role = await GetProductRoleAsync(productId);
            return role == UserRoleType.Owner;
        }

        /// <summary>
        /// Determines whether is license reader.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        public async Task<bool> IsLicenseReaderAsync(Guid licenseId)
        {
            var productId = await _dataStore.Set<LicenseEntity>()
                .AsNoTracking().Where(x => x.Id == licenseId)
                .Select(x => x.ProductId).FirstOrDefaultAsync();
            if (productId != default)
            {
                var role = await GetProductRoleAsync(productId);
                return role != UserRoleType.None;
            }
            return false;
        }

        /// <summary>
        /// Determines whether is license manager.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        public async Task<bool> IsLicenseManagerAsync(Guid licenseId)
        {
            var productId = await _dataStore.Set<LicenseEntity>()
                .AsNoTracking().Where(x => x.Id == licenseId)
                .Select(x => x.ProductId).FirstOrDefaultAsync();
            if (productId != default)
            {
                var role = await GetProductRoleAsync(productId);
                return role == UserRoleType.Owner;
            }
            return false;
        }

        #endregion Authorization Component

        #region Permission handler

        /// <summary>
        /// Gets the current user's tenant role.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns></returns>
        private async Task<UserRoleType> GetTenantRoleAsync(Guid tenantId)
        {
            var currentUser = await _userService.GetAsync();
            var cacheKey = $"permission-{currentUser}-{tenantId}";
            var userRole = await GetCacheRoleAsync(cacheKey);
            if (userRole == UserRoleType.None)
            {
                userRole = await _dataStore.Set<PermissionEntity>()
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.ProductId == null && x.UserId == currentUser.Id)
                    .Select(x => x.Role)
                    .FirstOrDefaultAsync();
                await SetCacheRoleAsync(cacheKey, userRole);
            }
            return userRole;
        }

        /// <summary>
        /// Gets the current user's product role
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <returns></returns>
        private async Task<UserRoleType> GetProductRoleAsync(Guid productId)
        {
            var currentUser = await _userService.GetAsync();
            var cacheKey = $"permission-{currentUser}-{productId}";
            var userRole = await GetCacheRoleAsync(cacheKey);
            if (userRole == UserRoleType.None)
            {
                var tenantId = await _dataStore.Set<ProductEntity>()
                    .AsNoTracking()
                    .Where(x => x.Id == productId)
                    .Select(x => x.TenantId)
                    .FirstOrDefaultAsync();
                if (tenantId == default)
                    return UserRoleType.None;

                userRole = await _dataStore.Set<PermissionEntity>()
                    .AsNoTracking()
                    .Where(x => x.UserId == currentUser.Id && x.TenantId == tenantId && (x.ProductId == productId || x.ProductId == null))
                    .Select(x => x.Role)
                    .OrderByDescending(x => (int)x)
                    .FirstOrDefaultAsync();
                if (userRole == default)
                    return UserRoleType.None;

                await SetCacheRoleAsync(cacheKey, userRole);
            }
            return userRole;
        }

        #endregion Permission handler

        #region Authorization Cache

        /// <summary>
        /// Sets the user role in the distributed cache.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="userRole">The user role.</param>
        private async Task SetCacheRoleAsync(string cacheKey, UserRoleType userRole)
            => await SetCacheAsync(cacheKey, ((int)userRole).ToString(), TimeSpan.FromMinutes(1));

        /// <summary>
        /// Gets the user role result from the distributed cache.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        private async Task<UserRoleType> GetCacheRoleAsync(string cacheKey)
        {
            var result = await _cache.GetAsync(cacheKey.GetHashCode().ToString());
            if (result != null)
            {
                var value = Encoding.UTF8.GetString(result);
                return (UserRoleType)int.Parse(value);
            }
            return UserRoleType.None;
        }

        /// <summary>
        /// Sets the value in the distributed cache.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="value">The value.</param>
        /// <param name="timeSpan">The time span.</param>
        private async Task SetCacheAsync(string cacheKey, string value, TimeSpan timeSpan)
        {
            await _cache.SetAsync(
                key: cacheKey.GetHashCode().ToString(),
                value: Encoding.UTF8.GetBytes(value),
                options: new DistributedCacheEntryOptions().SetSlidingExpiration(timeSpan));
        }

        #endregion Authorization Cache
    }
}