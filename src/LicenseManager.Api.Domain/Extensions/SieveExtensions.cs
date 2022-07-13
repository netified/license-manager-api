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

using LicenseManager.Api.Abstractions;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Extensions
{
    public static class SieveExtensions
    {
        /// <summary>
        /// Apply sieve formatting to queryable request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sieveProcessor"></param>
        /// <param name="query"></param>
        /// <param name="sieveModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<PagedResult<T>> GetPagedAsync<T>(this ISieveProcessor sieveProcessor, IQueryable<T> query, SieveModel sieveModel = null, CancellationToken cancellationToken = default) where T : class
        {
            var result = new PagedResult<T>();

            var (pagedQuery, page, pageSize, rowCount, pageCount) = await GetPagedResultAsync(sieveProcessor, query, sieveModel, cancellationToken);

            result.CurrentPage = page;
            result.PageSize = pageSize;
            result.RowCount = rowCount;
            result.PageCount = pageCount;
            result.Results = await pagedQuery.ToListAsync(cancellationToken);

            return result;
        }

        /// <summary>
        /// Internal method to retrieve data from datastore with sieve formatting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sieveProcessor"></param>
        /// <param name="query"></param>
        /// <param name="sieveModel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<(IQueryable<T> pagedQuery, int page, int pageSize, int rowCount, int pageCount)> GetPagedResultAsync<T>(ISieveProcessor sieveProcessor, IQueryable<T> query, SieveModel sieveModel = null, CancellationToken cancellationToken = default) where T : class
        {
            var page = sieveModel?.Page ?? 1;
            var pageSize = sieveModel?.PageSize ?? 50;

            if (sieveModel != null)
            {
                // apply pagination in a later step
                query = sieveProcessor.Apply(sieveModel, query, applyPagination: false);
            }

            var rowCount = await query.CountAsync(cancellationToken);

            var pageCount = (int)Math.Ceiling((double)rowCount / pageSize);

            var skip = (page - 1) * pageSize;
            var pagedQuery = query.Skip(skip).Take(pageSize);

            return (pagedQuery, page, pageSize, rowCount, pageCount);
        }
    }
}