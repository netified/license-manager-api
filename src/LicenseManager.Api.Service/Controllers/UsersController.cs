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
    /// User Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
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
        /// 🧊 List all users.
        /// </summary>
        /// <remarks>
        /// Returns a list of all users for this application. \
        /// This endpoint allows you to sort, filter and add paging features to retrieve data the way you want it. \
        /// Please read the official documentation for more information about the filtering function.
        /// </remarks>
        /// <param name="filters">The filters.</param>
        /// <param name="sorts">The sorts.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<UserDto>>> ListAsync(string? filters, string? sorts, int? page = 1, int? pageSize = 100, CancellationToken stoppingToken = default)
        {
            var entities = await _userService.ListAsync(filters, sorts, page, pageSize, stoppingToken);
            return Ok(_mapper.Map<PagedResult<UserDto>>(entities));
        }

        /// <summary>
        /// 🧊 Get the current user.
        /// </summary>
        /// <remarks>
        /// Retrieves information about the user who is currently authenticated. \
        /// In the case of a client-side authenticated OAuth 2.0 application, this will be the user who authorized the application.
        /// </remarks>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> GetCurrentAsync(CancellationToken stoppingToken)
        {
            var entity = await _userService.GetAsync(stoppingToken);
            return Ok(_mapper.Map<UserDto>(entity));
        }

        /// <summary>
        /// 🧊 Update the default tenant of the current user.
        /// </summary>
        /// <remarks>
        /// The default tenant is used for the administration website.
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPut("me/tenants/{tenantId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> SetDefaultTenantAsync(Guid tenantId, CancellationToken stoppingToken)
        {
            await _userService.SetDefaultTenantAsync(tenantId, stoppingToken);
            return NoContent();
        }
    }
}