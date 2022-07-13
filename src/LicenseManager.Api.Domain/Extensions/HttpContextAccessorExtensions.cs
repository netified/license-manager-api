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
using LicenseManager.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace LicenseManager.Api.Domain.Extensions;

public static class HttpContextAccessorExtensions
{
    /// <summary>
    /// Gets the display name from the http context.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="identityConfiguration">The identity configuration.</param>
    /// <returns>The user mail</returns>
    public static string GetHttpDisplayName(this IHttpContextAccessor contextAccessor, IdentityConfiguration identityConfiguration)
    {
        if (string.IsNullOrEmpty(identityConfiguration.DisplayName))
            throw new BadRequestExecption("The display name configuration null or empty, " +
                "please send the request with valid authentication");

        var user = contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new BadRequestExecption("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(identityConfiguration.DisplayName)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new BadRequestExecption("The display name is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    /// <summary>
    /// Gets the user name from the http context.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="identityConfiguration">The identity configuration.</param>
    /// <returns>The user name</returns>
    public static string GetHttpUserName(this IHttpContextAccessor contextAccessor, IdentityConfiguration identityConfiguration)
    {
        if (string.IsNullOrEmpty(identityConfiguration.UserName))
            throw new BadRequestExecption("The user name configuration null or empty, " +
                "please send the request with valid authentication");

        var user = contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new BadRequestExecption("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(identityConfiguration.UserName)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new BadRequestExecption("The user name is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    /// <summary>
    /// Gets the user mail from the http context.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="identityConfiguration">The identity configuration.</param>
    /// <returns>The user mail</returns>
    public static string GetHttpEmail(this IHttpContextAccessor contextAccessor, IdentityConfiguration identityConfiguration)
    {
        if (string.IsNullOrEmpty(identityConfiguration.Email))
            throw new BadRequestExecption("The user mail configuration null or empty, " +
                "please send the request with valid authentication");

        var user = contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new BadRequestExecption("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var claim = user!.FindFirst(identityConfiguration.Email)?.Value;
        if (string.IsNullOrEmpty(claim))
            throw new BadRequestExecption("The email address is null or empty, " +
                "please send the request with valid authentication");
        return claim;
    }

    /// <summary>
    /// Gets the effective user identifier from the http context.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="identityConfiguration">The identity configuration.</param>
    /// <returns>The effective user identifier</returns>
    public static string GetHttpIdentifier(this IHttpContextAccessor contextAccessor, IdentityConfiguration identityConfiguration)
    {
        if (string.IsNullOrEmpty(identityConfiguration.RemoteId))
            throw new BadRequestExecption("The remote identifier configuration null or empty, " +
                "please send the request with valid authentication");

        var user = contextAccessor.HttpContext?.User;
        if (user!.Identity?.IsAuthenticated == false)
            throw new BadRequestExecption("Unable to find authentication context, " +
                "please send the request with valid authentication");

        var stringIdentifier = user!.FindFirst(identityConfiguration.RemoteId)?.Value;
        if (string.IsNullOrEmpty(stringIdentifier))
            throw new BadRequestExecption("The name identifier is null or empty, " +
                "please send the request with valid authentication");
        return stringIdentifier;
    }
}