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
using Sieve.Models;
using System.ComponentModel.DataAnnotations;

namespace LicenseManager.Api.Service.Controllers
{
    /// <summary>
    /// Organization Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/organizations")]
    public class OrganizationsController : ControllerBase
    {
        private readonly OrganizationService _organizationService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationsController"/> class.
        /// </summary>
        /// <param name="organizationService">The organization service.</param>
        /// <param name="mapper">The mapper.</param>
        public OrganizationsController(OrganizationService organizationService, IMapper mapper)
        {
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// List all organizations.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">Returns a list of all organization objects.</response>
        /// <response code="401">Access to this resource requires authentication.</response>
        /// <returns>List of all organization objects.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Organization>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<Organization>>> ListAsync([FromQuery] SieveModel request, CancellationToken cancellationToken)
        {
            var entities = await _organizationService.ListAsync(request, cancellationToken);
            return Ok(_mapper.Map<PagedResult<Organization>>(entities));
        }

        /// <summary>
        /// Create organization.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="201">Returns a created organization object.</response>
        /// <response code="409">The current request conflicts with another organization.</response>
        /// <response code="401">Access to this resource requires authentication.</response>
        /// <returns>Single organization object.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Organization), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Organization>> AddAsync(OrganizationRequest request, CancellationToken cancellationToken)
        {
            var entity = await _organizationService.AddAsync(request, cancellationToken);
            var entityDto = _mapper.Map<Organization>(entity);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { organizationId = entity.Id },
                value: entityDto);
        }

        /// <summary>
        /// Get organization.
        /// </summary>
        /// <remarks>
        /// Retrieves information about a organization.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">Returns a organization object.</response>
        /// <response code="401">Access to this resource requires authentication.</response>
        /// <response code="404">The organization object do not exists.</response>
        /// <returns>Single organization object.</returns>
        [HttpGet("{organizationId:guid}")]
        [ProducesResponseType(typeof(Organization), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Organization>> GetAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var entity = await _organizationService.GetAsync(organizationId, cancellationToken);
            return Ok(_mapper.Map<Organization>(entity));
        }

        /// <summary>
        /// Remove organization.
        /// </summary>
        /// <remarks>
        /// Permanently deletes a organization.
        /// Only users with owner-level permissions will be able to use this API.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="204">A blank response is returned if the organization was successfully deleted.</response>
        /// <response code="401">Access to this resource requires authentication.</response>
        /// <response code="403">You do not have permission to remove this organization.</response>
        /// <response code="404">The organization object do not exists.</response>
        /// <returns>Single organization object.</returns>
        /// <returns>Nothing</returns>
        [HttpDelete("{organizationId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            await _organizationService.RemoveAsync(organizationId, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// List members of organization.
        /// </summary>
        /// <remarks>
        /// Retrieves all the members for a organization.
        /// Only members of this organization will be able to use this API.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpGet("{organizationId:guid}/members")]
        [ProducesResponseType(typeof(List<UserOrganization>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<UserOrganization>>> GetMemberAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            var entities = await _organizationService.GetMemberAsync(organizationId, cancellationToken);
            return Ok(_mapper.Map<List<UserOrganization>>(entities));
        }

        /// <summary>
        /// Add user to organization.
        /// </summary>
        /// <remarks>
        /// Creates a organization membership.
        /// Only owners of this organization will be able to use this API.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{organizationId:guid}/members")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> AddMemberAsync(Guid organizationId, OrganizationMemberRequest request, CancellationToken cancellationToken)
        {
            await _organizationService.AddMemberAsync(organizationId, request, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Remove user from organization
        /// </summary>
        /// <remarks>
        /// Deletes a specific organization membership.
        /// Only owners of this organization will be able to use this API.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{organizationId:guid}/members/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
        {
            await _organizationService.RemoveMemberAsync(organizationId, userId, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Updates role of an organization user.
        /// </summary>
        /// <remarks>
        /// Updates the role of an organization user
        /// Only owners of this organization will be able to use this API.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="role">The role.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{organizationId:guid}/members/{userId:guid}/role")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateRoleMemberAsync(Guid organizationId, Guid userId, [Required] OrganizationRole role, CancellationToken cancellationToken)
        {
            await _organizationService.UpdateRoleMemberAsync(organizationId, userId, role, cancellationToken);
            return NoContent();
        }
    }
}