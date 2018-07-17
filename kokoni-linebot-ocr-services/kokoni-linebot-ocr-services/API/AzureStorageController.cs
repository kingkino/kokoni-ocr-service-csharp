﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace KokoniLinebotOCRServices.Library
{
    public class AzureStorageController
    {
        public static async Task PutLineContentsToStorageAsync(Stream stream, string ContainerName, string PathWithFileName)
        {
            // ストレージアクセス情報の作成
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureStorageAccount"]);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // retry設定 3秒秒3回
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            var container = blobClient.GetContainerReference(ContainerName);

            await container.CreateIfNotExistsAsync();

            // ストレージアクセスポリシーの設定
            await container.SetPermissionsAsync(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Off,
                });

            // Blob へファイルをアップロード
            var blob = container.GetBlockBlobReference(PathWithFileName);

            await blob.UploadFromStreamAsync(stream);
        }

        /// <summary>
        /// 指定ストレージからコンテンツを取得
        /// </summary>
        /// <returns>Stream</returns>
        public static async Task<Stream> GetLineContentsFromStorageAsync(string ContainerName, string PathWithFileName)
        {
            // ストレージアクセス情報の作成
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureStorageAccount"]);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // retry設定 3秒3回
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            var container = blobClient.GetContainerReference(ContainerName);

            // Blob からダウンロード
            var blob = container.GetBlockBlobReference(PathWithFileName);

            var memoryStream = new MemoryStream();

            await blob.DownloadToStreamAsync(memoryStream);

            return memoryStream;
        }
    }
}
