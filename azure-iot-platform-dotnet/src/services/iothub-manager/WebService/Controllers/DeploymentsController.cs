// <copyright file="DeploymentsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeploymentsController : Controller
    {
        private readonly IDeployments deployments;

        public DeploymentsController(IDeployments deployments)
        {
            this.deployments = deployments;
        }

        [HttpPost]
        [Authorize("CreateDeployments")]
        public async Task<DeploymentApiModel> PostAsync([FromBody] DeploymentApiModel deployment)
        {
            if (string.IsNullOrWhiteSpace(deployment.Name))
            {
                throw new InvalidInputException("Name must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupId))
            {
                throw new InvalidInputException("DeviceGroupId must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupName))
            {
                throw new InvalidInputException("DeviceGroupName must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupQuery))
            {
                throw new InvalidInputException("DeviceGroupQuery must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.PackageContent))
            {
                throw new InvalidInputException("PackageContent must be provided");
            }

            if (deployment.PackageType.Equals(PackageType.DeviceConfiguration)
                && string.IsNullOrEmpty(deployment.ConfigType))
            {
                throw new InvalidInputException("Configuration type must be provided");
            }

            if (deployment.Priority < 0)
            {
                throw new InvalidInputException($"Invalid priority provided of {deployment.Priority}. " +
                                                "It must be non-negative");
            }

            return new DeploymentApiModel(await this.deployments.CreateAsync(deployment.ToServiceModel()));
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<DeploymentListApiModel> GetAsync()
        {
            return new DeploymentListApiModel(await this.deployments.ListAsync());
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<DeploymentApiModel> GetAsync(string id, [FromQuery] bool includeDeviceStatus = false)
        {
            return new DeploymentApiModel(await this.deployments.GetAsync(id, includeDeviceStatus));
        }

        [HttpDelete("{id}")]
        [Authorize("DeleteDeployments")]
        public async Task DeleteAsync(string id)
        {
            await this.deployments.DeleteAsync(id);
        }
    }
}