// <copyright file="Permissions.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Mmm.Iot.Common.Services.Auth
{
    public static class Permissions
    {
        public static readonly IReadOnlyDictionary<string, IEnumerable<string>> Roles = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Admin", new List<string>
                {
                    "AcquireToken",
                    "CreateDeployments",
                    "CreateDeviceGroups",
                    "CreateDevices",
                    "CreateJobs",
                    "CreatePackages",
                    "CreateRules",
                    "DeleteAlarms",
                    "DeleteDeployments",
                    "DeleteDeviceGroups",
                    "DeleteDevices",
                    "DeletePackages",
                    "DeleteRules",
                    "DeleteTenant",
                    "DisableAlerting",
                    "EnableAlerting",
                    "ReadAll",
                    "TagPackages",
                    "UpdateAlarms",
                    "UpdateDeviceGroups",
                    "UpdateDevices",
                    "UpdateRules",
                    "UpdateSimManagement",
                    "UserManage",
                    "SendC2DMessages",
                }
            },
            {
                "ReadOnly", new List<string>
                {
                    "ReadAll",
                }
            },
        };
    }
}