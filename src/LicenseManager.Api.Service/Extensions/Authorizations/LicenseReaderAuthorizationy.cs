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

using Microsoft.AspNetCore.Authorization;

namespace LicenseManager.Api.Service.Extensions.Authorizations
{
    public class LicenseReaderRequirement : IAuthorizationRequirement
    { }

    public class LicenseReaderHandler : AuthorizationHandler<LicenseReaderRequirement>
    {
        private readonly AuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseReaderHandler"/> class.
        /// </summary>
        /// <param name="authorizationService">The authorization service.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public LicenseReaderHandler(AuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Makes a decision if authorization is allowed based on a specific requirement.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The requirement to evaluate.</param>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LicenseReaderRequirement requirement)
        {
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == false)
            {
                context.Fail(new AuthorizationFailureReason(this, "You are not authenticated. Authentication required to perform this operation."));
                return;
            }

            var routeData = _httpContextAccessor!.HttpContext!.GetRouteData();
            if (!Guid.TryParse(routeData.Values["licenseId"]!.ToString()!, out Guid licenseId))
                context.Fail(new AuthorizationFailureReason(this, "Unable to retrieve a valid license identifier."));

            try
            {
                if (await _authorizationService.IsLicenseReaderAsync(licenseId))
                    context.Succeed(requirement);
                else
                    context.Fail(new AuthorizationFailureReason(this, "You are not authorized to read this license."));
            }
            catch (Exception ex) { context.Fail(new AuthorizationFailureReason(this, ex.Message)); }
        }
    }
}