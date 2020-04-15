// <copyright file="IBlobStorageClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.Common.Services;

namespace Mmm.Iot.AsaManager.Services.External.BlobStorage
{
    public interface IBlobStorageClient : IStatusOperation
    {
        Task CreateBlobAsync(string blobContainerName, string contentFileName, string blobFileName);
    }
}