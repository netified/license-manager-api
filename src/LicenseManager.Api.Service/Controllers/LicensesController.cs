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
    /// Product licenses controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/products/{productId:guid}/licenses")]
    public class LicensesController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly LicenseService _licenseService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicensesController"/> class.
        /// </summary>
        /// <param name="productService"></param>
        /// <param name="licenseService"></param>
        public LicensesController(ProductService productService, LicenseService licenseService)
        {
            _productService = productService;
            _licenseService = licenseService;
        }

        /// <summary>
        /// Get all product licenses.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Abstractions.License>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<Abstractions.License>>> ListAsync(Guid productId, [FromQuery] SieveModel request, CancellationToken cancellationToken)
        { 
            var entities = await _licenseService.ListAsync(productId, request, cancellationToken);
            return Ok(_mapper.Map<PagedResult<Abstractions.License>>(entities));
        }

        /// <summary>
        /// Get product license by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("{licenseId:guid}")]
        [ProducesResponseType(typeof(ICollection<Abstractions.Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Abstractions.License>> GetAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            var entity =await _licenseService.GetAsync(productId, licenseId, cancellationToken);
            return Ok(_mapper.Map<Abstractions.License>(entity));
        }

        /// <summary>
        /// Add product licenses.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Abstractions.License), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Abstractions.License>> AddAsync(Guid productId, LicenseRequest request, CancellationToken cancellationToken)
        {
            var entity = await _licenseService.AddAsync(productId, request, cancellationToken);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { productId = entity.ProductId, licenseId = entity.Id },
                value: _mapper.Map<Abstractions.License>(entity));
        }

        /// <summary>
        /// Import a product license.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="license"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(Abstractions.License), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ICollection<Abstractions.License>>> ImportAsync(Guid productId, LicenseBackup license, CancellationToken cancellationToken)
            => Ok(await _licenseService.ImportAsync(productId, license, cancellationToken));

        /// <summary>
        /// Export a product license by identifier.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("{licenseId:guid}/export")]
        [ProducesResponseType(typeof(LicenseBackup), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LicenseBackup>> ExportAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
            => Ok(await _licenseService.ExportAsync(productId, licenseId, cancellationToken));

        /// <summary>
        /// Generate a product license.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("{licenseId:guid}/generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GenerateAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            var product = await _productService.GetAsync(productId, cancellationToken);
            var license = await _licenseService.GenerateAsync(productId, licenseId, cancellationToken);
            using var memStream = new MemoryStream();
            license.Save(memStream);
            return File(memStream.ToArray(), "application/xml", $"{product.Name}-{licenseId}.xml".ToLower());
        }

        /// <summary>
        /// Delete a product license.
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="licenseId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpDelete("{licenseId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveAsync(Guid productId, Guid licenseId, CancellationToken cancellationToken)
        {
            await _licenseService.RemoveAsync(productId, licenseId, cancellationToken);
            return NoContent();
        }
    }
}