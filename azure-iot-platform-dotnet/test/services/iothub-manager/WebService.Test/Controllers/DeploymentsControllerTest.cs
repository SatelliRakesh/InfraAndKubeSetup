// <copyright file="DeploymentsControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Controllers;
using Mmm.Iot.IoTHubManager.WebService.Models;
using Moq;
using Xunit;

namespace Mmm.Iot.IoTHubManager.WebService.Test.Controllers
{
    public class DeploymentsControllerTest : IDisposable
    {
        private const string DeploymentName = "depname";
        private const string DeviceGroupId = "dvcGroupId";
        private const string DeviceGroupName = "dvcGroupName";
        private const string DeviceGroupQuery = "dvcGroupQuery";
        private const string PackageContent = "{}";
        private const string PackageName = "packageName";
        private const string DeploymentId = "dvcGroupId-packageId";
        private const int Priority = 10;
        private const string ConfigurationType = "Edge";
        private readonly DeploymentsController controller;
        private readonly Mock<IDeployments> deploymentsMock;
        private bool disposedValue = false;

        public DeploymentsControllerTest()
        {
            this.deploymentsMock = new Mock<IDeployments>();
            this.controller = new DeploymentsController(this.deploymentsMock.Object);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetDeploymentTest()
        {
            // Arrange
            this.deploymentsMock.Setup(x => x.GetAsync(DeploymentId, false)).ReturnsAsync(new DeploymentServiceModel()
            {
                Name = DeploymentName,
                DeviceGroupId = DeviceGroupId,
                DeviceGroupName = DeviceGroupName,
                DeviceGroupQuery = DeviceGroupQuery,
                PackageContent = PackageContent,
                PackageName = PackageName,
                Priority = Priority,
                Id = DeploymentId,
                PackageType = PackageType.EdgeManifest,
                ConfigType = ConfigurationType,
                CreatedDateTimeUtc = DateTime.UtcNow,
            });

            // Act
            var result = await this.controller.GetAsync(DeploymentId);

            // Assert
            Assert.Equal(DeploymentId, result.DeploymentId);
            Assert.Equal(DeploymentName, result.Name);
            Assert.Equal(PackageContent, result.PackageContent);
            Assert.Equal(PackageName, result.PackageName);
            Assert.Equal(DeviceGroupId, result.DeviceGroupId);
            Assert.Equal(DeviceGroupName, result.DeviceGroupName);
            Assert.Equal(Priority, result.Priority);
            Assert.Equal(PackageType.EdgeManifest, result.PackageType);
            Assert.Equal(ConfigurationType, result.ConfigType);
            Assert.True((DateTimeOffset.UtcNow - result.CreatedDateTimeUtc).TotalSeconds < 5);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyGroupAndPackageNameLabelsTest()
        {
            // Arrange
            this.deploymentsMock.Setup(x => x.GetAsync(DeploymentId, false)).ReturnsAsync(new DeploymentServiceModel()
            {
                Name = DeploymentName,
                DeviceGroupId = DeviceGroupId,
                DeviceGroupName = DeviceGroupName,
                DeviceGroupQuery = DeviceGroupQuery,
                PackageContent = PackageContent,
                Priority = Priority,
                Id = DeploymentId,
                PackageType = PackageType.EdgeManifest,
                ConfigType = ConfigurationType,
                CreatedDateTimeUtc = DateTime.UtcNow,
            });

            // Act
            var result = await this.controller.GetAsync(DeploymentId);

            // Assert
            Assert.Equal(DeploymentId, result.DeploymentId);
            Assert.Equal(DeploymentName, result.Name);
            Assert.Equal(PackageContent, result.PackageContent);
            Assert.Equal(DeviceGroupId, result.DeviceGroupId);
            Assert.Equal(Priority, result.Priority);
            Assert.Equal(PackageType.EdgeManifest, result.PackageType);
            Assert.Equal(ConfigurationType, result.ConfigType);
            Assert.True((DateTimeOffset.UtcNow - result.CreatedDateTimeUtc).TotalSeconds < 5);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetDeploymentsTest(int numDeployments)
        {
            // Arrange
            var deploymentsList = new List<DeploymentServiceModel>();
            var deploymentMetrics = new DeploymentMetricsServiceModel(null, null)
            {
                DeviceMetrics = new Dictionary<DeploymentStatus, long>()
                {
                    { DeploymentStatus.Succeeded, 0 },
                    { DeploymentStatus.Pending, 0 },
                    { DeploymentStatus.Failed, 0 },
                },
            };

            for (var i = 0; i < numDeployments; i++)
            {
                deploymentsList.Add(new DeploymentServiceModel()
                {
                    Name = DeploymentName + i,
                    DeviceGroupId = DeviceGroupId + i,
                    DeviceGroupQuery = DeviceGroupQuery + i,
                    PackageContent = PackageContent + i,
                    Priority = Priority + i,
                    Id = DeploymentId + i,
                    PackageType = PackageType.EdgeManifest,
                    ConfigType = ConfigurationType,
                    CreatedDateTimeUtc = DateTime.UtcNow,
                    DeploymentMetrics = deploymentMetrics,
                });
            }

            this.deploymentsMock.Setup(x => x.ListAsync()).ReturnsAsync(
                new DeploymentServiceListModel(deploymentsList));

            // Act
            var results = await this.controller.GetAsync();

            // Assert
            Assert.Equal(numDeployments, results.Items.Count);
            for (var i = 0; i < numDeployments; i++)
            {
                var result = results.Items[i];
                Assert.Equal(DeploymentId + i, result.DeploymentId);
                Assert.Equal(DeploymentName + i, result.Name);
                Assert.Equal(DeviceGroupQuery + i, result.DeviceGroupQuery);
                Assert.Equal(DeviceGroupId + i, result.DeviceGroupId);
                Assert.Equal(PackageContent + i, result.PackageContent);
                Assert.Equal(Priority + i, result.Priority);
                Assert.Equal(PackageType.EdgeManifest, result.PackageType);
                Assert.Equal(ConfigurationType, result.ConfigType);
                Assert.True((DateTimeOffset.UtcNow - result.CreatedDateTimeUtc).TotalSeconds < 5);
                Assert.Equal(5, result.Metrics.SystemMetrics.Count());
            }
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("depName", "dvcGroupId", "dvcQuery", "pkgContent", 10, false)]
        [InlineData("", "dvcGroupId", "dvcQuery", "pkgContent", 10, true)]
        [InlineData("depName", "", "dvcQuery", "pkgContent", 10, true)]
        [InlineData("depName", "dvcGroupId", "", "pkgContent", 10, true)]
        [InlineData("depName", "dvcGroupId", "dvcQuery", "", 10, true)]
        [InlineData("depName", "dvcGroupId", "dvcQuery", "pkgContent", -1, true)]
        public async Task PostDeploymentTest(
            string name,
            string deviceGroupId,
            string deviceGroupQuery,
            string packageContent,
            int priority,
            bool throwsException)
        {
            // Arrange
            var deploymentId = "test-deployment";
            const string deviceGroupName = "DeviceGroup";
            this.deploymentsMock.Setup(x => x.CreateAsync(Match.Create<DeploymentServiceModel>(model =>
                    model.DeviceGroupId == deviceGroupId &&
                    model.PackageContent == packageContent &&
                    model.Priority == priority &&
                    model.DeviceGroupName == deviceGroupName &&
                    model.Name == name &&
                    model.PackageType == PackageType.EdgeManifest &&
                    model.ConfigType == ConfigurationType)))
                .ReturnsAsync(new DeploymentServiceModel()
                {
                    Name = name,
                    DeviceGroupId = deviceGroupId,
                    DeviceGroupName = deviceGroupName,
                    DeviceGroupQuery = deviceGroupQuery,
                    PackageContent = packageContent,
                    Priority = priority,
                    Id = deploymentId,
                    PackageType = PackageType.EdgeManifest,
                    ConfigType = ConfigurationType,
                    CreatedDateTimeUtc = DateTime.UtcNow,
                });

            var depApiModel = new DeploymentApiModel()
            {
                Name = name,
                DeviceGroupId = deviceGroupId,
                DeviceGroupQuery = deviceGroupQuery,
                DeviceGroupName = deviceGroupName,
                PackageContent = packageContent,
                PackageType = PackageType.EdgeManifest,
                ConfigType = ConfigurationType,
                Priority = priority,
            };

            // Act
            if (throwsException)
            {
                await Assert.ThrowsAsync<InvalidInputException>(async () => await this.controller.PostAsync(depApiModel));
            }
            else
            {
                var result = await this.controller.PostAsync(depApiModel);

                // Assert
                Assert.Equal(deploymentId, result.DeploymentId);
                Assert.Equal(name, result.Name);
                Assert.Equal(deviceGroupId, result.DeviceGroupId);
                Assert.Equal(deviceGroupQuery, result.DeviceGroupQuery);
                Assert.Equal(packageContent, result.PackageContent);
                Assert.Equal(priority, result.Priority);
                Assert.Equal(PackageType.EdgeManifest, result.PackageType);
                Assert.Equal(ConfigurationType, result.ConfigType);
                Assert.True((DateTimeOffset.UtcNow - result.CreatedDateTimeUtc).TotalSeconds < 5);
            }
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("depName", "dvcGroupId", "dvcQuery", "pkgContent", -1)]
        public async Task PostInvalidDeploymentTest(
            string name,
            string deviceGroupId,
            string deviceGroupQuery,
            string packageContent,
            int priority)
        {
            // Arrange
            var depApiModel = new DeploymentApiModel()
            {
                Name = name,
                DeviceGroupId = deviceGroupId,
                DeviceGroupQuery = deviceGroupQuery,
                PackageContent = packageContent,
                PackageType = PackageType.DeviceConfiguration,
                ConfigType = string.Empty,
                Priority = priority,
            };

            // Act
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.controller.PostAsync(depApiModel));
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.controller.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}