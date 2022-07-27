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
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Services
{
    public class OrganizationService
    {
        private readonly DataStoreDbContext _dataStore;
        private readonly IdentityService _identityService;
        private readonly PermissionService _permissionService;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ILogger<OrganizationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationService"/> class.
        /// </summary>
        /// <param name="dataStore">The data store.</param>
        /// <param name="identityService">The identity service.</param>
        /// <param name="permissionService">The permission service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sieveProcessor">The sieve processor.</param>
        /// <param name="logger">The logger.</param>
        public OrganizationService(
            DataStoreDbContext dataStore,
            IdentityService identityService,
            PermissionService permissionService,
            IMapper mapper,
            ISieveProcessor sieveProcessor,
            ILogger<OrganizationService> logger)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieve the organization from the data store using the pagination functionality.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<PagedResult<OrganizationEntity>> ListAsync(SieveModel request, CancellationToken cancellationToken)
        {
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            var query = _dataStore.Set<OrganizationEntity>()
                .AsNoTracking()
                .Include(x => x.UserOrganizations)
                .Where(x => x.UserOrganizations.Any(x => x.UserId == userId))
                .AsSplitQuery();
            return await _sieveProcessor.GetPagedAsync(query, request, cancellationToken);
        }

        /// <summary>
        /// Create new organization.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<OrganizationEntity> AddAsync(OrganizationRequest request, CancellationToken cancellationToken)
        {
            // Detect if the request conflicts with the data store
            var detectConflit = await _dataStore.Set<OrganizationEntity>().AnyAsync(x => x.Name == request.Name, cancellationToken);
            if (detectConflit)
                throw new ConflitException($"The following organization name already exists: {request.Name}");

            // Map the organization request to organization
            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            var organization = _mapper.Map<OrganizationEntity>(request);
            organization.UserOrganizations = new List<UserOrganizationEntity>()
            {
                new UserOrganizationEntity()
                {
                    UserId = userId,
                    Role = OrganizationRoleType.Owner
                }
            };

            // Save data to the data store
            await _dataStore.Set<OrganizationEntity>().AddAsync(organization, cancellationToken);
            await _dataStore.SaveChangesAsync(userId, cancellationToken);
            _logger.LogDebug("The following organization has been created: {organizationName}", organization.Name);

            return organization;
        }

        /// <summary>
        /// Retrieve the organization from the data store.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<OrganizationEntity> GetAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanViewOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            // Retrieve the organization from the data store
            var organizationEntity = await _dataStore.Set<OrganizationEntity>()
                .Include(x => x.UserOrganizations)
                .Where(x => x.Id == organizationId)
                .FirstOrDefaultAsync(cancellationToken);

            // Return the organization if it exists
            return organizationEntity == default ?
                throw new NotFoundException(nameof(OrganizationEntity))
                : organizationEntity;
        }

        /// <summary>
        /// Delete the organization from the data store.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task RemoveAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanDeleteOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            // Retrieve the organization from the datastore
            var organizationEntity = await GetAsync(organizationId, cancellationToken);

            // Delete product in data store
            _dataStore.Set<OrganizationEntity>().Remove(organizationEntity);
            await _dataStore.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("The following product has been deleted: {organizationName}", organizationEntity.Name);
        }

        /// <summary>
        /// Gets the organization members from the data store.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<List<UserOrganizationEntity>> GetMemberAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanViewOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            return await _dataStore.Set<UserOrganizationEntity>()
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the role member in the organization by their identifiers.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="role">The role.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task UpdateRoleMemberAsync(Guid organizationId, Guid userId, OrganizationRoleType role, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanManageOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            var member = await _dataStore.Set<UserOrganizationEntity>()
                .Where(x => x.OrganizationId == organizationId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (member == default)
                throw new NotFoundException();

            member.Role = role;
            _dataStore.Set<UserOrganizationEntity>().Update(member);
            await _dataStore.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adds the organization member to the database.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public async Task AddMemberAsync(Guid organizationId, OrganizationMemberRequest request, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanManageOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            var member = await _dataStore.Set<UserOrganizationEntity>()
                .Where(x => x.OrganizationId == organizationId && x.UserId == request.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (member != default)
                throw new ConflitException();

            member = new UserOrganizationEntity()
            {
                OrganizationId = organizationId,
                UserId = request.UserId,
                Role = request.Role
            };

            var userId = await _identityService.GetIdentifierAsync(cancellationToken);
            await _dataStore.Set<UserOrganizationEntity>().AddAsync(member, cancellationToken);
            await _dataStore.SaveChangesAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Removes the organization member from the data store.
        /// </summary>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
        {
            if (!await _permissionService.CanManageOrganization(organizationId, cancellationToken))
                throw new UnauthorizedAccessException();

            var members = await _dataStore.Set<UserOrganizationEntity>()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
            if (members.Count == 1 && members.First().UserId == userId)
                await RemoveAsync(organizationId, cancellationToken);
            else
            {
                var member = members.Where(x => x.UserId == userId).FirstOrDefault();
                if (member == default)
                    throw new NotFoundException();

                _dataStore.Remove(members);
                await _dataStore.SaveChangesAsync(cancellationToken);
            }
        }
    }
}