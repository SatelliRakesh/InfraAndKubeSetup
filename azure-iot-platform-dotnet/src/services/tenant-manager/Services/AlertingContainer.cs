// <copyright file="AlertingContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.TenantManager.Services.Helpers;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services
{
    public class AlertingContainer : IAlertingContainer
    {
        private const string SaCosmosDbDatabaseName = "pcs-iothub-stream";
        private const string SaNameFormat = "sa-{0}";

        private readonly ITenantContainer tenantContainer;
        private readonly IStreamAnalyticsHelper streamAnalyticsHelper;
        private readonly IRunbookHelper runbookHelper;
        private readonly IStorageClient cosmosDb;

        public AlertingContainer(
            ITenantContainer tenantContainer,
            IStreamAnalyticsHelper streamAnalyticsHelper,
            IRunbookHelper runbookHelper,
            IStorageClient cosmosDb)
        {
            this.tenantContainer = tenantContainer;
            this.streamAnalyticsHelper = streamAnalyticsHelper;
            this.runbookHelper = runbookHelper;
            this.cosmosDb = cosmosDb;
        }

        public bool SaJobExists(StreamAnalyticsJobModel saJobModel)
        {
            return !string.IsNullOrEmpty(saJobModel.StreamAnalyticsJobName) && !string.IsNullOrEmpty(saJobModel.JobState);
        }

        public async Task<StreamAnalyticsJobModel> AddAlertingAsync(string tenantId)
        {
            // Attempt to create the necessary database if it does not already exist
            await this.cosmosDb.CreateDatabaseIfNotExistsAsync(SaCosmosDbDatabaseName);

            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (this.SaJobExists(saJobModel))
            {
                throw new Exception("The given tenant already has a deployed stream analytics job.");
            }

            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            string saJobName = string.Format(SaNameFormat, tenantId.Substring(0, 8));
            await this.runbookHelper.CreateAlerting(tenantId, saJobName, tenant.IotHubName);
            return new StreamAnalyticsJobModel
            {
                TenantId = tenant.TenantId,
                StreamAnalyticsJobName = saJobName,
                IsActive = false,
                JobState = "Creating",
            };
        }

        public async Task<StreamAnalyticsJobModel> RemoveAlertingAsync(string tenantId)
        {
            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            await this.runbookHelper.DeleteAlerting(tenantId, tenant.SAJobName);
            return new StreamAnalyticsJobModel
            {
                TenantId = tenant.TenantId,
                StreamAnalyticsJobName = tenant.SAJobName,
                IsActive = false,
                JobState = "Deleting",
            };
        }

        public async Task<StreamAnalyticsJobModel> GetAlertingAsync(string tenantId)
        {
            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            try
            {
                var job = await this.streamAnalyticsHelper.GetJobAsync(tenant.SAJobName);
                return new StreamAnalyticsJobModel
                {
                    TenantId = tenant.TenantId,
                    JobState = job.JobState,
                    IsActive = this.streamAnalyticsHelper.JobIsActive(job),
                    StreamAnalyticsJobName = job.Name,
                };
            }
            catch (ResourceNotFoundException)
            {
                // Return a model with null information regarding the stream analytics job if it does not exist
                return new StreamAnalyticsJobModel
                {
                    TenantId = tenant.TenantId,
                    IsActive = false,
                };
            }
            catch (Exception e)
            {
                throw new Exception("An Unknown exception occurred while attempting to get the tenant's stream analytics job.", e);
            }
        }

        public async Task<StreamAnalyticsJobModel> StartAlertingAsync(string tenantId)
        {
            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (!this.SaJobExists(saJobModel))
            {
                throw new Exception("There is no StreamAnalyticsJob is available to start for this tenant.");
            }

            await this.streamAnalyticsHelper.StartAsync(saJobModel.StreamAnalyticsJobName);
            return saJobModel;
        }

        public async Task<StreamAnalyticsJobModel> StopAlertingAsync(string tenantId)
        {
            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (!this.SaJobExists(saJobModel))
            {
                throw new Exception("There is no StreamAnalyticsJob is available to stop for this tenant.");
            }

            await this.streamAnalyticsHelper.StopAsync(saJobModel.StreamAnalyticsJobName);
            return saJobModel;
        }

        private async Task<TenantModel> GetTenantFromContainerAsync(string tenantId)
        {
            TenantModel tenant = await this.tenantContainer.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                throw new Exception("The given tenant does not exist.");
            }

            bool tenantReady = await this.tenantContainer.TenantIsReadyAsync(tenantId);
            if (!tenantReady)
            {
                throw new Exception("The tenant is not fully deployed yet. Please wait for the tenant to fully deploy before performing alerting operations");
            }

            return tenant;
        }
    }
}