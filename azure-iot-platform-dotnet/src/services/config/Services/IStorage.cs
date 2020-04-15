// <copyright file="IStorage.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mmm.Iot.Config.Services.External;
using Mmm.Iot.Config.Services.Models;
using Mmm.Platform.IoT.Common.Services.Models;

namespace Mmm.Iot.Config.Services
{
    public interface IStorage
    {
        Task<object> GetThemeAsync();

        Task<object> SetThemeAsync(object theme);

        Task<object> GetUserSetting(string id);

        Task<object> SetUserSetting(string id, object setting);

        Task<Logo> GetLogoAsync();

        Task<Logo> SetLogoAsync(Logo model);

        Task<IEnumerable<DeviceGroup>> GetAllDeviceGroupsAsync();

        Task<ConfigTypeListServiceModel> GetConfigTypesListAsync();

        Task<DeviceGroup> GetDeviceGroupAsync(string id);

        Task<DeviceGroup> CreateDeviceGroupAsync(DeviceGroup input);

        Task<DeviceGroup> UpdateDeviceGroupAsync(string id, DeviceGroup input, string etag);

        Task DeleteDeviceGroupAsync(string id);

        Task<IEnumerable<PackageServiceModel>> GetAllPackagesAsync();

        Task<PackageServiceModel> GetPackageAsync(string id);

        Task<IEnumerable<PackageServiceModel>> GetFilteredPackagesAsync(string packageType, string configType);

        Task<PackageServiceModel> AddPackageAsync(PackageServiceModel package);

        Task DeletePackageAsync(string id);

        Task UpdateConfigTypeAsync(string customConfigType);

        Task<UploadFileServiceModel> UploadToBlobAsync(string tenantId, string filename, Stream stream = null);

        Task<PackageServiceModel> AddPackageTagAsync(string id, string tag);

        Task<PackageServiceModel> RemovePackageTagAsync(string id, string tag);
    }
}