// <copyright file="Storage.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Config.Services.External;
using Mmm.Iot.Config.Services.Helpers.PackageValidation;
using Mmm.Iot.Config.Services.Models;
using Mmm.Platform.IoT.Common.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Config.Services
{
    public class Storage : IStorage
    {
        public const string SolutionCollectionId = "solution-settings";
        public const string ThemeKey = "theme";
        public const string LogoKey = "logo";
        public const string UserCollectionId = "user-settings";
        public const string DeviceGroupCollectionId = "devicegroups";
        public const string PackagesCollectionId = "packages";
        public const string PackagesConfigTypeKey = "config-types";
        public const string AzureMapsKey = "AzureMapsKey";
        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        public const string SoftwarePackageStore = "software-package";
        private readonly IStorageAdapterClient client;
        private readonly IAsaManagerClient asaManager;
        private readonly AppConfig config;
        private readonly ILogger logger;

        public Storage(
            IStorageAdapterClient client,
            IAsaManagerClient asaManager,
            AppConfig config,
            ILogger<Storage> logger)
        {
            this.client = client;
            this.asaManager = asaManager;
            this.config = config;
            this.logger = logger;
        }

        public async Task<object> GetThemeAsync()
        {
            string data;

            try
            {
                var response = await this.client.GetAsync(SolutionCollectionId, ThemeKey);
                data = response.Data;
            }
            catch (ResourceNotFoundException)
            {
                data = JsonConvert.SerializeObject(Theme.Default);
            }

            var themeOut = JsonConvert.DeserializeObject(data) as JToken ?? new JObject();
            this.AppendAzureMapsKey(themeOut);
            return themeOut;
        }

        public async Task<object> SetThemeAsync(object themeIn)
        {
            var value = JsonConvert.SerializeObject(themeIn);
            var response = await this.client.UpdateAsync(SolutionCollectionId, ThemeKey, value, "*");
            var themeOut = JsonConvert.DeserializeObject(response.Data) as JToken ?? new JObject();
            this.AppendAzureMapsKey(themeOut);
            return themeOut;
        }

        public async Task<object> GetUserSetting(string id)
        {
            try
            {
                var response = await this.client.GetAsync(UserCollectionId, id);
                return JsonConvert.DeserializeObject(response.Data);
            }
            catch (ResourceNotFoundException)
            {
                return new object();
            }
        }

        public async Task<object> SetUserSetting(string id, object setting)
        {
            var value = JsonConvert.SerializeObject(setting);
            var response = await this.client.UpdateAsync(UserCollectionId, id, value, "*");
            return JsonConvert.DeserializeObject(response.Data);
        }

        public async Task<Logo> GetLogoAsync()
        {
            try
            {
                var response = await this.client.GetAsync(SolutionCollectionId, LogoKey);
                return JsonConvert.DeserializeObject<Logo>(response.Data);
            }
            catch (ResourceNotFoundException)
            {
                return Logo.Default;
            }
        }

        public async Task<Logo> SetLogoAsync(Logo model)
        {
            // Do not overwrite existing name or image with null
            if (model.Name == null || model.Image == null)
            {
                Logo current = await this.GetLogoAsync();
                if (!current.IsDefault)
                {
                    model.Name = model.Name ?? current.Name;
                    if (model.Image == null && current.Image != null)
                    {
                        model.Image = current.Image;
                        model.Type = current.Type;
                    }
                }
            }

            var value = JsonConvert.SerializeObject(model);
            var response = await this.client.UpdateAsync(SolutionCollectionId, LogoKey, value, "*");
            return JsonConvert.DeserializeObject<Logo>(response.Data);
        }

        public async Task<IEnumerable<DeviceGroup>> GetAllDeviceGroupsAsync()
        {
            var response = await this.client.GetAllAsync(DeviceGroupCollectionId);
            return response.Items.Select(this.CreateGroupServiceModel);
        }

        public async Task<DeviceGroup> GetDeviceGroupAsync(string id)
        {
            var response = await this.client.GetAsync(DeviceGroupCollectionId, id);
            return this.CreateGroupServiceModel(response);
        }

        public async Task<DeviceGroup> CreateDeviceGroupAsync(DeviceGroup input)
        {
            var value = JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var response = await this.client.CreateAsync(DeviceGroupCollectionId, value);
            var responseModel = this.CreateGroupServiceModel(response);
            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return responseModel;
        }

        public async Task<DeviceGroup> UpdateDeviceGroupAsync(string id, DeviceGroup input, string etag)
        {
            var value = JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var response = await this.client.UpdateAsync(DeviceGroupCollectionId, id, value, etag);
            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return this.CreateGroupServiceModel(response);
        }

        public async Task DeleteDeviceGroupAsync(string id)
        {
            await this.client.DeleteAsync(DeviceGroupCollectionId, id);
            await this.asaManager.BeginDeviceGroupsConversionAsync();
        }

        public async Task<IEnumerable<PackageServiceModel>> GetAllPackagesAsync()
        {
            var response = await this.client.GetAllAsync(PackagesCollectionId);
            return response.Items.AsParallel().Where(r => r.Key != PackagesConfigTypeKey)
                .Select(this.CreatePackageServiceModel);
        }

        public async Task<IEnumerable<PackageServiceModel>> GetFilteredPackagesAsync(string packageType, string configType)
        {
            var response = await this.client.GetAllAsync(PackagesCollectionId);
            IEnumerable<PackageServiceModel> packages = response.Items.AsParallel()
                .Where(r => r.Key != PackagesConfigTypeKey)
                .Select(this.CreatePackageServiceModel);

            bool isPackageTypeEmpty = string.IsNullOrEmpty(packageType);
            bool isConfigTypeEmpty = string.IsNullOrEmpty(configType);

            if (!isPackageTypeEmpty && !isConfigTypeEmpty)
            {
                return packages.Where(p => (p.PackageType.ToString().Equals(packageType) &&
                                           p.ConfigType.Equals(configType)));
            }
            else if (!isPackageTypeEmpty && isConfigTypeEmpty)
            {
                return packages.Where(p => p.PackageType.ToString().Equals(packageType));
            }
            else if (isPackageTypeEmpty && !isConfigTypeEmpty)
            {
                // Non-empty ConfigType with empty PackageType indicates invalid packages
                throw new InvalidInputException("Package Type cannot be empty.");
            }
            else
            {
                // Return all packages when ConfigType & PackageType are empty
                return packages;
            }
        }

        public async Task<PackageServiceModel> AddPackageAsync(PackageServiceModel package)
        {
            bool isValidPackage = this.IsValidPackage(package);
            if (!isValidPackage)
            {
                var msg = "Package provided is a invalid deployment manifest " +
                    $"for type {package.PackageType}";

                msg += package.PackageType.Equals(PackageType.DeviceConfiguration) ?
                    $"and configuration {package.ConfigType}" : string.Empty;

                throw new InvalidInputException(msg);
            }

            try
            {
                JsonConvert.DeserializeObject<Configuration>(package.Content);
            }
            catch (Exception)
            {
                throw new InvalidInputException("Package provided is not a valid deployment manifest");
            }

            package.DateCreated = DateTimeOffset.UtcNow.ToString(DateFormat);
            var value = JsonConvert.SerializeObject(
                package,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            var response = await this.client.CreateAsync(PackagesCollectionId, value);

            if (!string.IsNullOrEmpty(package.ConfigType) && package.PackageType.Equals(PackageType.DeviceConfiguration))
            {
                await this.UpdateConfigTypeAsync(package.ConfigType);
            }

            return this.CreatePackageServiceModel(response);
        }

        public async Task DeletePackageAsync(string id)
        {
            await this.client.DeleteAsync(PackagesCollectionId, id);
        }

        public async Task<PackageServiceModel> GetPackageAsync(string id)
        {
            var response = await this.client.GetAsync(PackagesCollectionId, id);
            return this.CreatePackageServiceModel(response);
        }

        public async Task<ConfigTypeListServiceModel> GetConfigTypesListAsync()
        {
            try
            {
                var response = await this.client.GetAsync(PackagesCollectionId, PackagesConfigTypeKey);
                return JsonConvert.DeserializeObject<ConfigTypeListServiceModel>(response.Data);
            }
            catch (ResourceNotFoundException)
            {
                this.logger.LogDebug("Document config-types has not been created.");

                // Return empty Package Config types
                return new ConfigTypeListServiceModel();
            }
        }

        public async Task UpdateConfigTypeAsync(string customConfigType)
        {
            ConfigTypeListServiceModel list;
            try
            {
                var response = await this.client.GetAsync(PackagesCollectionId, PackagesConfigTypeKey);
                list = JsonConvert.DeserializeObject<ConfigTypeListServiceModel>(response.Data);
            }
            catch (ResourceNotFoundException)
            {
                this.logger.LogDebug("Config Types have not been created.");

                // Create empty Package Config Types
                list = new ConfigTypeListServiceModel();
            }

            list.Add(customConfigType);
            await this.client.UpdateAsync(PackagesCollectionId, PackagesConfigTypeKey, JsonConvert.SerializeObject(list), "*");
        }

        public async Task<UploadFileServiceModel> UploadToBlobAsync(string tenantId, string filename, Stream stream = null)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            UploadFileServiceModel uploadFileModel = new UploadFileServiceModel();

            string url = string.Empty;
            string md5CheckSum = string.Empty;
            string sha1CheckSum = string.Empty;
            string storageConnectionString = this.config.Global.StorageAccountConnectionString;
            string duration = this.config.Global.PackageSharedAccessExpiryTime;

            if (string.IsNullOrEmpty(tenantId))
            {
                this.logger.LogError(new Exception("Tenant ID is blank, cannot create container without tenandId."), "Tenant ID is blank, cannot create container without tenandId.");
                return null;
            }

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create a container
                    cloudBlobContainer = cloudBlobClient.GetContainerReference($"{tenantId}-{SoftwarePackageStore}");

                    // Create the container if it does not already exist
                    await cloudBlobContainer.CreateIfNotExistsAsync();

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);

                    if (stream != null)
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                        md5CheckSum = cloudBlockBlob.Properties.ContentMD5;
                        using (var sha = SHA1.Create())
                        {
                            var hash = sha.ComputeHash(stream);
                            cloudBlockBlob.Metadata["SHA1"] = sha1CheckSum = Convert.ToBase64String(hash);
                        }
                    }
                    else
                    {
                        this.logger.LogError(new Exception("Empty stream object in the UploadToBlob method."), "Empty stream object in the UploadToBlob method.");
                        return null;
                    }

                    url = Convert.ToString(this.GetBlobSasUri(cloudBlobClient, cloudBlobContainer.Name, filename, duration));
                    uploadFileModel.CheckSum = new CheckSumModel();
                    uploadFileModel.SoftwarePackageURL = url;
                    uploadFileModel.CheckSum.MD5 = md5CheckSum;
                    uploadFileModel.CheckSum.SHA1 = sha1CheckSum;
                    return uploadFileModel;
                }
                catch (StorageException ex)
                {
                    this.logger.LogError(new Exception($"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}"), $"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}");
                    return null;
                }
            }
            else
            {
                this.logger.LogError(new Exception("Error parsing CloudStorageAccount in UploadToBlob method"), "Error parsing CloudStorageAccount in UploadToBlob method");
                return null;
            }
        }

        public async Task<PackageServiceModel> AddPackageTagAsync(string id, string tag)
        {
            var existingPackage = await this.GetPackageAsync(id);
            if (string.IsNullOrEmpty(tag))
            {
                return existingPackage;
            }

            if (existingPackage.Tags == null)
            {
                existingPackage.Tags = new List<string>();
            }

            if (existingPackage.Tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase))
            {
                return existingPackage;
            }

            existingPackage.Tags.Add(tag);
            var value = JsonConvert.SerializeObject(
                existingPackage,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });
            var response = await this.client.UpdateAsync(PackagesCollectionId, id, value, existingPackage.ETag);
            return this.CreatePackageServiceModel(response);
        }

        public async Task<PackageServiceModel> RemovePackageTagAsync(string id, string tag)
        {
            var existingPackage = await this.GetPackageAsync(id);
            if (string.IsNullOrEmpty(tag))
            {
                return existingPackage;
            }

            if (existingPackage?.Tags == null)
            {
                return existingPackage;
            }

            var existingTag = existingPackage.Tags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (existingTag == null)
            {
                return existingPackage;
            }

            existingPackage.Tags.Remove(existingTag);
            var value = JsonConvert.SerializeObject(
                existingPackage,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });
            var response = await this.client.UpdateAsync(PackagesCollectionId, id, value, existingPackage.ETag);
            return this.CreatePackageServiceModel(response);
        }

        private string GetBlobSasUri(CloudBlobClient cloudBlobClient, string containerName, string blobName, string timeoutDuration)
        {
            string[] time = timeoutDuration.Split(':');
            CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(Convert.ToDouble(time[0])).AddMinutes(Convert.ToDouble(time[1])).AddSeconds(Convert.ToDouble(time[2]));
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            // Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);

            // Return the URI string for the container, including the SAS token.
            return blockBlob.Uri + sasBlobToken;
        }

        private bool IsValidPackage(PackageServiceModel package)
        {
            IPackageValidator validator = PackageValidatorFactory.GetValidator(
                package.PackageType,
                package.ConfigType);

            // Bypass validation for custom _config type
            return validator == null || validator.Validate();
        }

        private DeviceGroup CreateGroupServiceModel(ValueApiModel input)
        {
            var output = JsonConvert.DeserializeObject<DeviceGroup>(input.Data);
            output.Id = input.Key;
            output.ETag = input.ETag;
            return output;
        }

        private PackageServiceModel CreatePackageServiceModel(ValueApiModel input)
        {
            var output = JsonConvert.DeserializeObject<PackageServiceModel>(input.Data);
            output.Id = input.Key;
            output.ETag = input.ETag;
            if (output.Tags == null)
            {
                output.Tags = new List<string>();
            }

            return output;
        }

        private void AppendAzureMapsKey(JToken theme)
        {
            if (theme[AzureMapsKey] == null)
            {
                theme[AzureMapsKey] = this.config.ConfigService.AzureMapsKey;
            }
        }
    }
}