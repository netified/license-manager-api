using AutoMapper;
using LicenseManager.Api.Abstractions;
using LicenseManager.Api.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseManager.Api.Domain.Profiles
{
    /// <summary>
    /// Product configuration for maps.
    /// </summary>
    public class PermissionProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionProfile"/> class.
        /// </summary>
        public PermissionProfile()
        {
            CreateMap<PermissionEntity, PermissionDto>()
                .ForMember(dest => dest.Type, o => o.MapFrom(src => src.ProductId == default ? "Global" : "Scoped"));
        }
    }
}
