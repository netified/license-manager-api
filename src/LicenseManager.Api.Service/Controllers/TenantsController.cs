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
using LicenseManager.Api.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.Api.Service.Controllers
{
    /// <summary>
    /// Tenant Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly TenantService _tenantService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantsController"/> class.
        /// </summary>
        /// <param name="tenantService">The tenant service.</param>
        /// <param name="mapper">The mapper.</param>
        public TenantsController(TenantService tenantService, IMapper mapper)
        {
            _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 🧊 List all tenants.
        /// </summary>
        /// <remarks>
        /// This endpoint allows you to sort, filter and add paging features to retrieve data the way you want it. \
        /// Please read the official documentation for more information about the filtering function.
        /// </remarks>
        /// <param name="filters">The filters.</param>
        /// <param name="sorts">The sorts.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<TenantDto>>> ListAsync(string? filters, string? sorts, int? page = 1, int? pageSize = 100, CancellationToken stoppingToken = default)
        {
            var entities = await _tenantService.ListAsync(filters, sorts, page, pageSize, stoppingToken);
            return Ok(_mapper.Map<PagedResult<TenantDto>>(entities));
        }

        /// <summary>
        /// 🧊 Create a tenant.
        /// </summary>
        /// <remarks>
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="request">The request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "Prenium")]
        [HttpPost]
        [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<TenantDto>> AddAsync(TenantRequest request, CancellationToken stoppingToken = default)
        {
            var entity = await _tenantService.AddAsync(request, stoppingToken);
            var entityDto = _mapper.Map<TenantDto>(entity);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { tenantId = entity.Id },
                value: entityDto);
        }

        /// <summary>
        /// 🧊 Getting a tenant.
        /// </summary>
        /// <remarks>
        /// Only the tenant's users will be able to use this API.
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "TenantReader")]
        [HttpGet("{tenantId:guid}")]
        [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<TenantDto>> GetAsync(Guid tenantId, CancellationToken stoppingToken = default)
        {
            var entity = await _tenantService.GetAsync(tenantId, stoppingToken);
            return Ok(_mapper.Map<TenantDto>(entity));
        }

        /// <summary>
        /// 🧊 Delete the tenant.
        /// </summary>
        /// <remarks>
        /// Only users with owner-level permissions will be able to use this API. \
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "TenantManager")]
        [HttpDelete("{tenantId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> RemoveAsync(Guid tenantId, CancellationToken stoppingToken = default)
        {
            await _tenantService.RemoveAsync(tenantId, stoppingToken);
            return NoContent();
        }

        /// <summary>
        /// 🧊 List of tenant's user permissions.
        /// </summary>
        /// <remarks>
        /// List of Tenant Members. \
        /// Only the tenant's users will be able to use this API.
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "TenantReader")]
        [HttpGet("{tenantId:guid}/permissions")]
        [ProducesResponseType(typeof(List<UserTenantDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserTenantDto>>> ListPermissionAsync(Guid tenantId, CancellationToken stoppingToken = default)
        {
            var entities = await _tenantService.ListPermissionAsync(tenantId, stoppingToken);
            return Ok(_mapper.Map<List<UserTenantDto>>(entities));
        }

        /// <summary>
        /// 🧊 Add a user permission to the tenant.
        /// </summary>
        /// <remarks>
        /// Only the owners of this tenant will be able to use this API. \
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="request">The request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "TenantManager")]
        [HttpPost("{tenantId:guid}/permissions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> AddPermissionAsync(Guid tenantId, TenantMemberRequest request, CancellationToken stoppingToken = default)
        {
            await _tenantService.AddPermissionAsync(tenantId, request, stoppingToken);
            return NoContent();
        }

        /// <summary>
        /// 🧊 Remove the user's permission from the tenant.
        /// </summary>
        /// <remarks>
        /// Only the owners of this tenant will be able to use this API. \
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "TenantManager")]
        [HttpDelete("{tenantId:guid}/permissions/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> RemovePermissionAsync(Guid tenantId, Guid userId, CancellationToken stoppingToken = default)
        {
            await _tenantService.RemovePermissionAsync(tenantId, userId, stoppingToken);
            return NoContent();
        }
    }
}