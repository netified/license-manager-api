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
    /// Product licenses controller.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}")]
    public class LicensesController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly LicenseService _licenseService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LicensesController"/> class.
        /// </summary>
        /// <param name="productService">The product service.</param>
        /// <param name="licenseService">The license service.</param>
        /// <param name="mapper">The mapper.</param>
        public LicensesController(ProductService productService, LicenseService licenseService, IMapper mapper)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 🧊 Get all licenses for a product.
        /// </summary>
        /// <remarks>
        /// This endpoint allows you to sort, filter and add paging features to retrieve data the way you want it. \
        /// Please read the official documentation for more information about the filtering function.
        /// </remarks>
        /// <param name="productId">The product identifier.</param>
        /// <param name="filters">The filters.</param>
        /// <param name="sorts">The sorts.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("products/{productId:guid}/licenses")]
        [Authorize(Policy = "ProductReader")]
        [ProducesResponseType(typeof(PagedResult<LicenseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<LicenseDto>>> ListAsync(Guid productId, string? filters, string? sorts, int? page = 1, int? pageSize = 100, CancellationToken stoppingToken = default)
        {
            var entities = await _licenseService.ListAsync(productId, filters, sorts, page, pageSize, stoppingToken);
            return Ok(_mapper.Map<PagedResult<LicenseDto>>(entities));
        }

        /// <summary>
        /// 🧊 Getting a license.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("licenses/{licenseId:guid}")]
        [Authorize(Policy = "LicenseReader")]
        [ProducesResponseType(typeof(ICollection<LicenseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<LicenseDto>> GetAsync(Guid licenseId, CancellationToken stoppingToken = default)
        {
            var entity = await _licenseService.GetAsync(licenseId, stoppingToken);
            return Ok(_mapper.Map<LicenseDto>(entity));
        }

        /// <summary>
        /// 🧊 Add a license to the product.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="request">The request.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPost("products/{productId:guid}/licenses")]
        [Authorize(Policy = "ProductManager")]
        [ProducesResponseType(typeof(LicenseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<LicenseDto>> AddAsync(Guid productId, LicenseRequest request, CancellationToken stoppingToken = default)
        {
            var entity = await _licenseService.AddAsync(productId, request, stoppingToken);
            return CreatedAtAction(
                actionName: nameof(GetAsync),
                routeValues: new { productId = entity.ProductId, licenseId = entity.Id },
                value: _mapper.Map<LicenseDto>(entity));
        }

        /// <summary>
        /// 🧊 Import a license into the product.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="license">The license.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPost("products/{productId:guid}/licenses/import")]
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "ProductManager")]
        [ProducesResponseType(typeof(LicenseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<LicenseDto>> ImportAsync(Guid productId, LicenseBackupDto license, CancellationToken stoppingToken = default)
            => Ok(await _licenseService.ImportAsync(productId, license, stoppingToken));

        /// <summary>
        /// 🧊 Export a license.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpPost("licenses/{licenseId:guid}/export")]
        [Authorize(Policy = "Prenium")]
        [Authorize(Policy = "LicenseManager")]
        [ProducesResponseType(typeof(LicenseBackupDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<LicenseBackupDto>> ExportAsync(Guid licenseId, CancellationToken stoppingToken = default)
            => Ok(await _licenseService.ExportAsync(licenseId, stoppingToken));

        /// <summary>
        /// 🧊 Download the license.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpGet("licenses/{licenseId:guid}/download")]
        [Authorize(Policy = "LicenseManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GenerateAsync(Guid licenseId, CancellationToken stoppingToken = default)
        {
            var license = await _licenseService.GenerateAsync(licenseId, stoppingToken);
            using var memStream = new MemoryStream();
            license.Save(memStream);
            return File(memStream.ToArray(), "application/xml", $"{license.Product.Name}-{licenseId}.xml".ToLower());
        }

        /// <summary>
        /// 🧊 Delete a license.
        /// </summary>
        /// <param name="licenseId">The license identifier.</param>
        /// <param name="stoppingToken">The cancellation token.</param>
        [HttpDelete("licenses/{licenseId:guid}")]
        [Authorize(Policy = "LicenseManager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> RemoveAsync(Guid licenseId, CancellationToken stoppingToken = default)
        {
            await _licenseService.RemoveAsync(licenseId, stoppingToken);
            return NoContent();
        }
    }
}