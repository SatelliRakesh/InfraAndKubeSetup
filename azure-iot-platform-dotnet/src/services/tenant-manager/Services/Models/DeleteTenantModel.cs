// <copyright file="DeleteTenantModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class DeleteTenantModel
    {
        private string tenantId;
        private bool ensuredDeployment;
        private Dictionary<string, bool> deletionRecord;

        public DeleteTenantModel()
        {
        }

        public DeleteTenantModel(string tenantGuid, Dictionary<string, bool> deletionRecord, bool ensuredDeployment)
        {
            this.tenantId = tenantGuid;
            this.ensuredDeployment = ensuredDeployment;
            this.deletionRecord = deletionRecord;
        }

        public bool FullyDeleted
        {
            get
            {
                return this.deletionRecord.All(item => item.Value);
            }
        }
    }
}