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

using FluentValidation.AspNetCore;
using LicenseManager.Api.Configuration;
using LicenseManager.Api.Data.Shared.DbContexts;
using LicenseManager.Api.Domain.Services;
using LicenseManager.Api.Service.Extensions;
using LicenseManager.Api.Service.Helpers;
using Sieve.Models;
using Sieve.Services;
using System.Text.Json.Serialization;

namespace LicenseManager.Api.Service
{
    public class Startup
    {
        /// <summary>
        /// Represents a set of key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add authentication
            services.AddJwtAuthentication(Configuration);

            // Add swagger generator
            services.AddSwagger(Configuration);

            // Add service for controllers.
            var controllers = services.AddControllers();
            controllers.AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            controllers.AddFluentValidation(s => s.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(p => !p.IsDynamic)));
            controllers.AddMvcOptions(opts => opts.SuppressAsyncSuffixInActionNames = false);

            // Frameworks
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddHttpContextAccessor();

            // Add sieve implementation
            services.AddScoped<SieveProcessor>();
            services.AddScoped<ISieveProcessor, SieveProcessorConfigurator>();
            services.Configure<SieveOptions>(Configuration.GetSection("SieveConfiguration"));

            // Register configuration
            var productConfiguration = new ApplicationConfiguration(Configuration);
            services.AddSingleton(productConfiguration);

            // Add DbContexts
            services.RegisterDbContexts<DataStoreDbContext, DataProtectionDbContext>(productConfiguration);
            services.AddDataProtection<DataProtectionDbContext>(productConfiguration);

            // Add cache system
            services.AddDistributedMemoryCache();

            // Add internal services
            services.AddTransient<ProductService>();
            services.AddTransient<LicenseService>();
            services.AddTransient<IdentityService>();
            services.AddTransient<PermissionService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<UserService>();
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Checks if the current host environment name is development.
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            // Enables routing capabilities
            app.UseRouting();

            // Add wagger implementation
            app.UseSwaggerUI(Configuration);

            // Enable authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Endpoint Configuration
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}