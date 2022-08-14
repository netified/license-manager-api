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
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Exceptions;
using LicenseManager.Api.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services
{
    public class TenantService
    {
        private readonly DataStoreDbContext _dataStore;
        private readonly UserService _userService;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantService"/> class.
        /// </summary>
        /// <param name="dataStore">The data store.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sieveProcessor">The sieve processor.</param>

        public TenantService(DataStoreDbContext dataStore, UserService userService, IMapper mapper, ISieveProcessor sieveProcessor)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        }

        /// <summary>
        /// Retrieve the tenant from the data store using the pagination functionality.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<PagedResult<TenantEntity>> ListAsync(string? filters, string? sorts, int? page, int? pageSize, CancellationToken stoppingToken = default)
        {
            var request = new SieveModel() { Filters = filters, Sorts = sorts, Page = page, PageSize = pageSize };
            var userId = await _userService.GetIdentifierAsync(stoppingToken);
            var query = _dataStore.Set<TenantEntity>()
                .AsNoTracking()
                .Include(x => x.Permissions)
                .Where(x => x.Permissions.Any(x => x.UserId == userId));
            return await _sieveProcessor.GetPagedAsync(query, request, stoppingToken);
        }

        /// <summary>
        /// Create new tenant.
        /// </summary>
        /// <param name="request">The tenant request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<TenantEntity> AddAsync(TenantRequest request, CancellationToken stoppingToken)
        {
            var detectConflit = await _dataStore.Set<TenantEntity>().AnyAsync(x => x.Name == request.Name, stoppingToken);
            if (detectConflit)
                throw new ConflitException($"The following tenant already exists: {request.Name}");

            var createdBy = await _userService.GetIdentifierAsync(stoppingToken);
            var tenantEntity = _mapper.Map<TenantEntity>(request);
            tenantEntity.Type = TenantType.Shared;
            tenantEntity.Permissions = new List<PermissionEntity>()
            {
                new PermissionEntity()
                {
                    UserId = createdBy,
                    Role = UserRoleType.Owner
                }
            };

            await _dataStore.Set<TenantEntity>().AddAsync(tenantEntity, stoppingToken);
            await _dataStore.SaveChangesAsync(createdBy, stoppingToken);
            return tenantEntity;
        }

        /// <summary>
        /// Retrieve the tenant from the data store.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<TenantEntity> GetAsync(Guid tenantId, CancellationToken stoppingToken)
        {
            var tenantEntity = await _dataStore.Set<TenantEntity>()
                .Include(x=>x.Permissions)
                .Where(x => x.Id == tenantId)
                .FirstOrDefaultAsync(stoppingToken);

            return tenantEntity ?? throw new NotFoundException($"Unable to find following tenant: {tenantId}");
        }

        /// <summary>
        /// Delete the tenant from the data store.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        public async Task RemoveAsync(Guid tenantId, CancellationToken stoppingToken)
        {
            var tenantEntity = await _dataStore.Set<TenantEntity>()
                .Include(x => x.Products)
                .Where(x => x.Id == tenantId)
                .FirstOrDefaultAsync(stoppingToken);
            if (tenantEntity.Type == TenantType.Personal)
                throw new BadRequestExecption("Unable to delete personal tenant");

            if (tenantEntity.Products.Count != 0)
                throw new BadRequestExecption("Please delete all products before requesting tenant deletion");

            _dataStore.Set<TenantEntity>().Remove(tenantEntity);
            await _dataStore.SaveChangesAsync(
                userId: await _userService.GetIdentifierAsync(stoppingToken),
                cancellationToken: stoppingToken);
        }

        /// <summary>
        /// Gets the tenant permissions from the data store.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<List<PermissionEntity>> ListPermissionAsync(Guid tenantId, CancellationToken stoppingToken)
            => await _dataStore.Set<PermissionEntity>()
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.TenantId == tenantId && x.ProductId == null)
                .ToListAsync(stoppingToken);

        /// <summary>
        /// Adds the tenant permission to the database.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="request">The permission request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        public async Task AddPermissionAsync(Guid tenantId, TenantMemberRequest request, CancellationToken stoppingToken)
        {
            var permissions = await _dataStore.Set<PermissionEntity>()
                .Include(x => x.Tenant)
                .Where(x => x.TenantId == tenantId && x.ProductId == null)
                .ToListAsync(stoppingToken);

            if (permissions[0].Tenant.Type == TenantType.Personal)
                throw new BadRequestExecption("Unable to update personal tenant");

            if (permissions.Any(x => x.UserId == request.UserId))
                throw new ConflitException("The user is already in the tenant");

            var permission = new PermissionEntity()
            {
                TenantId = tenantId,
                UserId = request.UserId,
                Role = request.Role
            };

            await _dataStore.Set<PermissionEntity>().AddAsync(permission, stoppingToken);
            await _dataStore.SaveChangesAsync(
                userId: await _userService.GetIdentifierAsync(stoppingToken),
                cancellationToken: stoppingToken);
        }

        /// <summary>
        /// Removes the tenant permission from the data store.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="permissionId">The permission identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        public async Task RemovePermissionAsync(Guid tenantId, Guid permissionId, CancellationToken stoppingToken)
        {
            var permissions = await _dataStore.Set<PermissionEntity>()
                .Include(x => x.Tenant)
                .Where(x => x.TenantId == tenantId && x.ProductId == null)
                .ToListAsync(stoppingToken);

            if (permissions[0].Tenant.Type == TenantType.Personal)
                throw new BadRequestExecption("Unable to update personal tenant");

            if (permissions.Count == 1 && permissions[0].Role == UserRoleType.Owner)
                throw new BadRequestExecption("Unable to remove last tenant owner");

            var permission = permissions.Find(x => x.Id == permissionId);
            if (permission == default)
                throw new NotFoundException("Unable to find permission");

            _dataStore.Remove(permission);
            await _dataStore.SaveChangesAsync(
                userId: await _userService.GetIdentifierAsync(stoppingToken),
                cancellationToken: stoppingToken);
        }
    }
}