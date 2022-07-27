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
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

namespace LicenseManager.Api.Service.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Add Swagger Generator.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            // Get swagger configurations.
            var swaggerConfiguration = new SwaggerConfiguration();
            configuration.GetSection(nameof(SwaggerConfiguration)).Bind(swaggerConfiguration);

            services.AddApiVersioning(setup =>
            {
                setup.DefaultApiVersion = new ApiVersion(1, 0);
                setup.AssumeDefaultVersionWhenUnspecified = true;
                setup.ReportApiVersions = true;
            });

            services.AddVersionedApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });

            services.AddSwaggerGen(c =>
                {
                    #region Documents

                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Software license management API",
                        Description = "Software license management solution for to easily protect your applications.",
                        Contact = new OpenApiContact
                        {
                            Name = "Thomas ILLIET",
                            Email = "contact@thomas-illiet.fr",
                            Url = new Uri("https://github.com/netified/")
                        }
                    });

                    #endregion Documents

                    #region Oauth2

                    c.AddSecurityDefinition("Oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Description = "Standard authorisation using the Oauth2 scheme.",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Scheme = "Bearer",
                        BearerFormat = "JWT",

                        Flows = new OpenApiOAuthFlows
                        {             
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = swaggerConfiguration.AuthorizationUrl,
                                TokenUrl = swaggerConfiguration.TokenUrl,
                                Scopes = swaggerConfiguration.Scopes,
                            }
                        }
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {{
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Oauth2"
                                }
                            },
                            new string[] {}
                        }});

                    #endregion Oauth2

                    // Describe all parameters, regardless of how they appear in code, in camelCase.
                    c.DescribeAllParametersInCamelCase();

                    // Set the comments path for the Swagger JSON and UI.
                    var xmlFile = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                        c.IncludeXmlComments(xmlPath, true);
                });
        }

        /// <summary>
        /// Register the Swagger middleware.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configuration">The configuration.</param>
        public static void UseSwaggerUI(this IApplicationBuilder app, IConfiguration configuration)
        {
            // Get swagger configurations.
            var swaggerConfiguration = new SwaggerConfiguration();
            configuration.GetSection(nameof(SwaggerConfiguration)).Bind(swaggerConfiguration);

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "/docs/{documentname}/swagger.json";
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // Global configuration.
                c.EnableFilter();
                c.DocExpansion(DocExpansion.None);
                c.DisplayRequestDuration();
                c.RoutePrefix = "docs";

                // Set OAuth configuration.
                c.OAuthClientId(swaggerConfiguration.ClientId);
                if(!string.IsNullOrEmpty(swaggerConfiguration.ClientSecret))
                    c.OAuthClientSecret(swaggerConfiguration.ClientSecret);
                c.OAuthScopes(swaggerConfiguration.Scopes?.Keys.ToArray());
                c.OAuthUsePkce();

                // Add swagger endpoint.
                c.SwaggerEndpoint($"{swaggerConfiguration.Prefix}/docs/v1/swagger.json", "License Manager - V1");
            });
        }
    }
}