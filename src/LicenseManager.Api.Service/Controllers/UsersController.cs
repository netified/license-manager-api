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

namespace LicenseManager.Api.Service.Controllers
{
    /// <summary>
    /// User Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// The user service
        /// </summary>
        private readonly UserService _userService;

        /// <summary>
        /// The mapper service
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="userService">The user service.</param>
        /// <param name="mapper">The mapper.</param>
        public UsersController(UserService userService, IMapper mapper)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// List application users.
        /// </summary>
        /// <remarks>
        /// Returns a list of all users for this application.
        /// </remarks>
        /// <param name="request">The filter request</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">Returns a list of all user objects</response>
        /// <response code="401">Access to this resource requires authentication</response>
        /// <returns>List of all user objects</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<UserDto>>> ListAsync([FromQuery] SieveModel request, CancellationToken cancellationToken)
        {
            var entities = await _userService.ListAsync(request, cancellationToken);
            return Ok(_mapper.Map<PagedResult<UserDto>>(entities));
        }

        /// <summary>
        /// Get current user.
        /// </summary>
        /// <remarks>
        /// Retrieves information about the user who is currently authenticated.
        /// In the case of a client-side authenticated OAuth 2.0 application this will be the user who authorized the app.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">Returns a single user object</response>
        /// <response code="401">Access to this resource requires authentication</response>
        /// <returns>Single user object</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> GetCurrentAsync(CancellationToken cancellationToken)
        {
            var entity = await _userService.GetCurrentAsync(cancellationToken);
            return Ok(_mapper.Map<UserDto>(entity));
        }

        /// <summary>
        /// Update current user default organization.
        /// </summary>
        /// <remarks>
        /// The default organization is used for the administration website.
        /// </remarks>
        /// <param name="organizationId">The organization identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="204">A blank response is returned if the user was successfully updated.</response>
        /// <response code="401">Access to this resource requires authentication</response>
        /// <returns>Nothing</returns>
        [HttpPut("me/organizations/{organizationId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> SetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            await _userService.SetDefaultOrganizationAsync(organizationId, cancellationToken);
            return NoContent();
        }
    }
}