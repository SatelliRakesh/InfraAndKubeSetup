// <copyright file="RequestExtension.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mmm.Iot.Common.Services
{
    public static class RequestExtension
    {
        public const string ContextKeyUserClaims = "CurrentUserClaims";
        public const string ContextKeyTenantId = "TenantID";
        public const string HeaderKeyTenantId = "ApplicationTenantID";
        public const string UserObjectIdClaimType = "sub";
        public const string ClaimKeyTenantId = "tenant";
        public const string RoleClaimType = "role";
        private const string ContextKeyAuthRequired = "AuthRequired";
        private const string ContextKeyAllowedActions = "CurrentUserAllowedActions";
        private const string ContextKeyExternalRequest = "ExternalRequest";

        public static void SetCurrentUserClaims(this HttpRequest request, IEnumerable<Claim> claims)
        {
            request.HttpContext.Items[ContextKeyUserClaims] = claims;
        }

        public static IEnumerable<Claim> GetCurrentUserClaims(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(ContextKeyUserClaims))
            {
                return new List<Claim>();
            }

            return request.HttpContext.Items[ContextKeyUserClaims] as IEnumerable<Claim>;
        }

        public static void SetAuthRequired(this HttpRequest request, bool authRequired)
        {
            request.HttpContext.Items[ContextKeyAuthRequired] = authRequired;
        }

        public static bool GetAuthRequired(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(ContextKeyAuthRequired))
            {
                return true;
            }

            return (bool)request.HttpContext.Items[ContextKeyAuthRequired];
        }

        public static void SetExternalRequest(this HttpRequest request, bool external)
        {
            request.HttpContext.Items[ContextKeyExternalRequest] = external;
        }

        public static bool IsExternalRequest(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(ContextKeyExternalRequest))
            {
                return true;
            }

            return (bool)request.HttpContext.Items[ContextKeyExternalRequest];
        }

        public static string GetCurrentUserObjectId(this HttpRequest request)
        {
            var claims = GetCurrentUserClaims(request);
            return claims.Where(c => c.Type.ToLowerInvariant().Equals(UserObjectIdClaimType, StringComparison.CurrentCultureIgnoreCase))
                .Select(c => c.Value).First();
        }

        public static IEnumerable<string> GetCurrentUserRoleClaim(this HttpRequest request)
        {
            var claims = GetCurrentUserClaims(request);
            return claims.Where(c => c.Type.ToLowerInvariant().Equals(RoleClaimType, StringComparison.CurrentCultureIgnoreCase))
                .Select(c => c.Value);
        }

        public static void SetCurrentUserAllowedActions(this HttpRequest request, IEnumerable<string> allowedActions)
        {
            request.HttpContext.Items[ContextKeyAllowedActions] = allowedActions;
        }

        public static IEnumerable<string> GetCurrentUserAllowedActions(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(ContextKeyAllowedActions))
            {
                return new List<string>();
            }

            return request.HttpContext.Items[ContextKeyAllowedActions] as IEnumerable<string>;
        }

        public static string GetTenant(this HttpRequest request)
        {
            if (!request.HttpContext.Items.ContainsKey(ContextKeyTenantId))
            {
                return null;
            }

            return request.HttpContext.Items[ContextKeyTenantId] as string;
        }

        public static void SetTenant(this HttpRequest request, ILogger logger)
        {
            string tenantId = null;
            if (IsExternalRequest(request))
            {
                logger.LogDebug("Request is external");
                var currentUserClaims = GetCurrentUserClaims(request);
                logger.LogDebug("Request contains {claimCount} claims: {claimKeys}", currentUserClaims?.Count(), string.Join(", ", currentUserClaims?.Select(claim => claim.Type)));
                if (currentUserClaims.Any(t => t.Type == ClaimKeyTenantId))
                {
                    tenantId = GetCurrentUserClaims(request).First(t => t.Type == ClaimKeyTenantId).Value;
                    logger.LogDebug("Setting tenant ID to {tenantId} from claim {claimType}", tenantId, ClaimKeyTenantId);
                }
                else
                {
                    tenantId = null;
                    logger.LogDebug("Setting tenant ID to null because claim {claimType} was not found", ClaimKeyTenantId);
                }
            }
            else
            {
                logger.LogDebug("Request is internal");
                logger.LogDebug("Request contains {headerCount} headers: {headerNames}", request.Headers.Count, string.Join(", ", request.Headers.Keys));
                if (request.Headers.ContainsKey(HeaderKeyTenantId))
                {
                    tenantId = request.Headers[HeaderKeyTenantId];
                    logger.LogDebug("Setting tenant ID to {tenantId} from header {headerName}", tenantId, HeaderKeyTenantId);
                }
                else
                {
                    tenantId = null;
                    logger.LogDebug("Setting tenant ID to null because header {headerName} was not found", HeaderKeyTenantId);
                }
            }

            SetTenant(request, tenantId);
        }

        public static void SetTenant(this HttpRequest request, string tenantId)
        {
            request.HttpContext.Items.Add(new KeyValuePair<object, object>(ContextKeyTenantId, tenantId));
        }
    }
}