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
using LicenseManager.Api.Data.Entities;
using LicenseManager.Api.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;

namespace LicenseManager.Api.Service.Controllers
{
    /// <summary>
    /// Product Controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/products")]
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
        /// Get all products.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<Product>>> ListAsync([FromQuery] SieveModel request, CancellationToken cancellationToken)
        {
            var entities = await _productService.ListAsync(request, cancellationToken);
            return Ok(_mapper.Map<PagedResult<Product>>(entities));
        }

        /// <summary>
        /// Add a new product.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Product>> AddAsync(ProductRequest request, CancellationToken cancellationToken)
        {
            var entity = await _productService.AddAsync(request, cancellationToken);
            var entityDto = _mapper.Map<Product>(entity);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { productId = entity.Id },
                value: entityDto);
        }

        /// <summary>
        /// Import a product with form data.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProductEntity>> ImportAsync(ProductBackup product, CancellationToken cancellationToken, bool checksumValidation = true)
        {
            var entity = await _productService.ImportAsync(product, checksumValidation, cancellationToken);
            return Ok(_mapper.Map<Product>(entity));
        }

        /// <summary>
        /// Get product by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("{productId:guid}")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Product>> GetAsync(Guid productId, CancellationToken cancellationToken)
        {
            var entity = await _productService.GetAsync(productId, cancellationToken);
            return Ok(_mapper.Map<Product>(entity));
        }

        /// <summary>
        /// Update product by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPut("{productId:guid}")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Product>> UpdateAsync(Guid productId, ProductRequest request, CancellationToken cancellationToken)
        {
            var entity = await _productService.UpdateAsync(productId, request, cancellationToken);
            return Ok(_mapper.Map<Product>(entity));
        }

        /// <summary>
        /// Export a product by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("{productId:guid}/export")]
        [ProducesResponseType(typeof(ProductBackup), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductBackup>> ExportAsync(Guid productId, CancellationToken cancellationToken)
            => Ok(await _productService.ExportAsync(productId, cancellationToken));

        /// <summary>
        /// Delete a product by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpDelete("{productId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAsync(Guid productId, CancellationToken cancellationToken)
        {
            await _productService.RemoveAsync(productId, cancellationToken);
            return NoContent();
        }
    }
}