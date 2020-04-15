// <copyright file="Invitation.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Mmm.Iot.IdentityGateway.Services.Models
{
    public class Invitation
    {
        [JsonProperty("email_address")]
        public string EmailAddress { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }
}