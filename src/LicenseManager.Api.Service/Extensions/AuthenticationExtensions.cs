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
using LicenseManager.Api.Service.Extensions.Authorizations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Logging;

namespace LicenseManager.Api.Service.Extensions
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Adds the JWT authentication.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Get authentication configurations.
            var authConfiguration = new AuthenticationConfiguration();
            configuration.GetSection(nameof(AuthenticationConfiguration)).Bind(authConfiguration);

            // Flag which indicates whether or not PII is shown in logs.
            IdentityModelEventSource.ShowPII = true;

            // Configure authentication.
            services.AddAuthentication(x => x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authConfiguration.Authority;
                options.RequireHttpsMetadata = authConfiguration.RequireHttpsMetadata;
                options.Audience = authConfiguration.Audience;
            });

            services.AddTransient<AuthorizationService>();
            services.AddTransient<IAuthorizationHandler, TenantReaderHandler>();
            services.AddTransient<IAuthorizationHandler, TenantManagerHandler>();
            services.AddTransient<IAuthorizationHandler, ProductReaderHandler>();
            services.AddTransient<IAuthorizationHandler, ProductManagerHandler>();
            services.AddTransient<IAuthorizationHandler, LicenseReaderHandler>();
            services.AddTransient<IAuthorizationHandler, LicenseManagerHandler>();
            services.AddTransient<IAuthorizationHandler, MemberHandler>();
            services.AddTransient<IAuthorizationHandler, PreniumHandler>();

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = ConfigureDefaults(new AuthorizationPolicyBuilder()).Build();
                options.AddPolicy("TenantReader", TenantReaderPolicy);
                options.AddPolicy("TenantManager", TenantManagerPolicy);
                options.AddPolicy("ProductReader", ProductReaderPolicy);
                options.AddPolicy("ProductManager", ProductManagerPolicy);
                options.AddPolicy("LicenseReader", LicenseReaderPolicy);
                options.AddPolicy("LicenseManager", LicenseManagerPolicy);
                options.AddPolicy("Prenium", PreniumPolicy);
            });
        }

        /// <summary>
        /// Gets the tenant acces policy.
        /// </summary>
        public static AuthorizationPolicy TenantReaderPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new TenantReaderRequirement())
            .Build();

        /// <summary>
        /// Gets the tenant manage policy.
        /// </summary>
        public static AuthorizationPolicy TenantManagerPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new TenantManagerRequirement())
            .Build();

        /// <summary>
        /// Gets the product acces policy.
        /// </summary>
        public static AuthorizationPolicy ProductReaderPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new ProductReaderRequirement())
            .Build();

        /// <summary>
        /// Gets the product manage policy.
        /// </summary>
        public static AuthorizationPolicy ProductManagerPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new ProductManagerRequirement())
            .Build();

        /// <summary>
        /// Gets the license acces policy.
        /// </summary>
        public static AuthorizationPolicy LicenseReaderPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new LicenseReaderRequirement())
            .Build();

        /// <summary>
        /// Gets the license manage policy.
        /// </summary>
        public static AuthorizationPolicy LicenseManagerPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new LicenseManagerRequirement())
            .Build();

        /// <summary>
        /// Gets the prenium policy.
        /// </summary>
        public static AuthorizationPolicy PreniumPolicy =>
            ConfigureDefaults(new AuthorizationPolicyBuilder())
            .AddRequirements(new PreniumRequirement())
            .Build();

        /// <summary>
        /// Configures the defaults policy.
        /// </summary>
        /// <param name="builder">The builder.</param>
        private static AuthorizationPolicyBuilder ConfigureDefaults(AuthorizationPolicyBuilder builder)
            => builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new MemberRequirement());
    }
}