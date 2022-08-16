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
    /// Product Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductsController"/> class.
        /// </summary>
        /// <param name="productService">The product service.</param>
        /// <param name="mapper">The mapper.</param>
        public ProductsController(ProductService productService, IMapper mapper)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 🧊 List all the products.
        /// </summary>
        /// <remarks>
        /// This endpoint allows you to sort, filter and add paging features to retrieve data the way you want it. \
        /// Please read the official documentation for more information about the filtering function.
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <param name="sorts">The sorts.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("tenants/{tenantId}/products")]
        [Authorize(Policy = "TenantReader")]
        [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductDto>>> ListAsync(Guid tenantId, string? filters, string? sorts, int? page = 1, int? pageSize = 100, CancellationToken stoppingToken = default)
        {
            var entities = await _productService.ListAsync(tenantId, filters, sorts, page, pageSize, stoppingToken);
            return Ok(_mapper.Map<PagedResult<ProductDto>>(entities));
        }

        /// <summary>
        /// 🧊 Add a product.
        /// </summary>
        /// <remarks>
        /// Only owner of the tenant or the product will be able to use this API.
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="request">The product request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPost("tenants/{tenantId}/products")]
        [Authorize(Policy = "TenantManager")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProductDto>> AddAsync(Guid tenantId, ProductRequest request, CancellationToken stoppingToken = default)
        {
            var entity = await _productService.AddAsync(tenantId, request, stoppingToken);
            var entityDto = _mapper.Map<ProductDto>(entity);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { productId = entity.Id },
                value: entityDto);
        }

        /// <summary>
        /// 🧊 Import a product.
        /// </summary>
        /// <remarks>
        /// Only owner of the tenant or the product will be able to use this API. \
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="product">The product configuration.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPost("tenants/{tenantId}/products/import")]
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "TenantManager")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductDto>> ImportAsync(Guid tenantId, ProductBackupDto product, CancellationToken stoppingToken = default)
        {
            var entity = await _productService.ImportAsync(tenantId, product, stoppingToken);
            return Ok(_mapper.Map<ProductDto>(entity));
        }

        /// <summary>
        /// 🧊 List of product's user permissions.
        /// </summary>
        /// <remarks>
        /// List of product members. \
        /// Only the product's users will be able to use this API.
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "ProductReader")]
        [HttpGet("products/{productId:guid}/permissions")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PermissionDto>>> ListPermissionAsync(Guid productId, CancellationToken stoppingToken = default)
        {
            var entities = await _productService.ListPermissionAsync(productId, stoppingToken);
            return Ok(_mapper.Map<List<PermissionDto>>(entities));
        }

        /// <summary>
        /// 🧊 Get a product.
        /// </summary>
        /// <remarks>
        /// Only users of the tenant or the product will be able to use this API.
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("products/{productId:guid}")]
        [Authorize(Policy = "ProductReader")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductDto>> GetAsync(Guid productId, CancellationToken stoppingToken = default)
        {
            var entity = await _productService.GetAsync(productId, stoppingToken);
            return Ok(_mapper.Map<ProductDto>(entity));
        }

        /// <summary>
        /// 🧊 Update the product configuration.
        /// </summary>
        /// <remarks>
        /// Only users with owner-level permissions will be able to use this API.
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="request">The product request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "ProductManager")]
        [HttpPut("products/{productId:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductDto>> UpdateAsync(Guid productId, ProductRequest request, CancellationToken stoppingToken = default)
        {
            var entity = await _productService.UpdateAsync(productId, request, stoppingToken);
            return Ok(_mapper.Map<ProductDto>(entity));
        }

        /// <summary>
        /// 🧊 Export a product.
        /// </summary>
        /// <remarks>
        /// Only users with owner-level permissions will be able to use this API. \
        /// ***( Prenium Feature )***
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "ProductManager")]
        [HttpPost("products/{productId:guid}/export")]
        [ProducesResponseType(typeof(ProductBackupDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductBackupDto>> ExportAsync(Guid productId, CancellationToken stoppingToken = default)
            => Ok(await _productService.ExportAsync(productId, stoppingToken));

        /// <summary>
        /// 🧊 Delete a product.
        /// </summary>
        /// <remarks>
        /// Permanently delete the product. \
        /// Only users with owner-level permissions will be able to use this API.
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [Authorize(Policy = "ProductManager")]
        [HttpDelete("products/{productId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteAsync(Guid productId, CancellationToken stoppingToken = default)
        {
            await _productService.RemoveAsync(productId, stoppingToken);
            return NoContent();
        }
    }
}