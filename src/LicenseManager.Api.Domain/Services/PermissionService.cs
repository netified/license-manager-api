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

using LicenseManager.Api.Data.Shared.DbContexts;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services
{
    public class PermissionService
    {
        #region Service

        private readonly IdentityService _identityService;
        private readonly DataStoreDbContext _dataStore;
        private readonly IDistributedCache _cache;

        #endregion Service

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionService"/> class.
        /// </summary>
        /// <param name="identityService">The identity service.</param>
        /// <param name="dataStore">The data store.</param>
        /// <param name="cache">The cache.</param>
        public PermissionService(IdentityService identityService, DataStoreDbContext dataStore, IDistributedCache cache)
        {
            _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #region Organization

        public async Task<bool> CanViewOrganization(Guid productId, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            return true;
        }

        public async Task<bool> CanManageOrganization(Guid productId, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            return true;
        }

        public async Task<bool> CanDeleteOrganization(Guid organizationId, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            return true;
        }

        #endregion Organization

        #region Product

        public async Task<bool> CanViewProduct(Guid productId, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            return true;
        }

        public async Task<bool> CanManageProduct(Guid productId, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            return true;
        }

        #endregion Product

        #region Cache

        /// <summary>
        /// Gets the value from cache asynchronous.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<string> GetCacheAsync(string key, CancellationToken cancellationToken)
        {
            var cacheValue = await _cache.GetAsync(key, cancellationToken);
            if (cacheValue != null)
                return Encoding.UTF8.GetString(cacheValue);
            return null;
        }

        /// <summary>
        /// Adds the value to the cache asynchronous.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task AddCacheAsync(string key, string value, CancellationToken cancellationToken)
        {
            var cacheValue = Encoding.UTF8.GetBytes(value);
            var options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(30));
            await _cache.SetAsync(key, cacheValue, options, cancellationToken);
        }

        #endregion Cache
    }
}