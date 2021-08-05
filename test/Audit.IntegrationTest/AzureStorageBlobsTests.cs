#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0
using Audit.Core;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audit.IntegrationTest
{
    public class AzureStorageBlobsTests
    {
        [Test]
        [Category("AzureStorageBlobs")]
        public void Test_AzureStorageBlobs_ConnectionString()
        {
            var id = Guid.NewGuid().ToString();
            var originalId = id;
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithConnectionString(AzureSettings.AzureBlobCnnString)
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Audit.Core.Configuration.ResetCustomActions();
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Audit.Core.Configuration.DataProvider = dp;

            using (var scope = AuditScope.Create("Test", () => id, new { custom = 123 }))
            {
                id = "changed!";
            }

            var result = dp.GetEvent<AuditEvent>(containerName, "Test" + "_" + originalId + ".json");
            var result2 = dp.GetEvent<AuditEvent>(containerName, "NotExists.json");

            Assert.IsNotNull(result);
            Assert.IsNull(result2);
            Assert.AreEqual("123", result.CustomFields["custom"].ToString());
            Assert.AreEqual("changed!", result.Target.New.ToString());
            Assert.AreEqual(originalId, result.Target.Old.ToString());
            Assert.AreEqual("Test", result.EventType);
        }

        [Test]
        [Category("AzureStorageBlobs")]
        public void Test_AzureStorageBlobs_Credential()
        {
            var id = Guid.NewGuid().ToString();
            var originalId = id;
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithCredentials(_ => _
                    .Url(AzureSettings.AzureBlobServiceUrl)
                    .Credential(new StorageSharedKeyCredential(AzureSettings.AzureBlobAccountName, AzureSettings.AzureBlobAccountKey)))
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Audit.Core.Configuration.ResetCustomActions();
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Audit.Core.Configuration.DataProvider = dp;

            using (var scope = AuditScope.Create("Test", () => id, new { custom = 123 }))
            {
                id = "changed!";
            }

            var result = dp.GetEvent<AuditEvent>(containerName, "Test" + "_" + originalId + ".json");
            var result2 = dp.GetEvent<AuditEvent>(containerName, "NotExists.json");

            Assert.IsNotNull(result);
            Assert.IsNull(result2);
            Assert.AreEqual("123", result.CustomFields["custom"].ToString());
            Assert.AreEqual("changed!", result.Target.New.ToString());
            Assert.AreEqual(originalId, result.Target.Old.ToString());
            Assert.AreEqual("Test", result.EventType);
        }

    }
}
#endif
