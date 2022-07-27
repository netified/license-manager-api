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

using LicenseManager.Api.Configuration;
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services
{
    public class IdentityService
    {
        #region Service

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IDistributedCache _cache;
        private readonly DataStoreDbContext _dataStore;
        private readonly ApplicationConfiguration _appConfiguration;
        private readonly UserService _userService;

        #endregion Service

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityService"/> class.
        /// </summary>
        /// <param name="contextAccessor">The context accessor.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="dataStore">The data store.</param>
        /// <param name="appConfiguration">The application configuration.</param>
        /// <param name="userService">The user service.</param>
        public IdentityService(IHttpContextAccessor contextAccessor, IDistributedCache cache, DataStoreDbContext dataStore, ApplicationConfiguration appConfiguration, UserService userService)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// Gets the effective user identifier.
        /// </summary>
        /// <returns>The effective user identifier</returns>
        public async Task<Guid> GetIdentifierAsync(CancellationToken cancellationToken)
        {
            var remoteIdentifier = _contextAccessor.GetHttpIdentifier(_appConfiguration.Identity);
            var cacheKey = nameof(UserEntity) + "-" + remoteIdentifier;
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

            return await _userService.RegisterAsync(cancellationToken);
        }
    }
}