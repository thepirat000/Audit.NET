using Audit.Core;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.IntegrationTest
{
    public class AzureStorageBlobsTests
    {
        public static string AzureBlobCnnString => Environment.GetEnvironmentVariable("AUDIT_NET_AZUREBLOBCNNSTRING") ?? throw new Exception($"Missing environment variable AUDIT_NET_AZUREBLOBCNNSTRING");
        public static string AzureBlobServiceUrl => Environment.GetEnvironmentVariable("AUDIT_NET_AZUREBLOBSERVICEURL") ?? throw new Exception($"Missing environment variable AUDIT_NET_AZUREBLOBSERVICEURL");
        public static string AzureBlobAccountName => Environment.GetEnvironmentVariable("AUDIT_NET_AZUREBLOBACCOUNTNAME") ?? throw new Exception($"Missing environment variable AUDIT_NET_AZUREBLOBACCOUNTNAME");
        public static string AzureBlobAccountKey => Environment.GetEnvironmentVariable("AUDIT_NET_AZUREBLOBACCOUNTKEY") ?? throw new Exception($"Missing environment variable AUDIT_NET_AZUREBLOBACCOUNTKEY");

        [Test]
        [Category("AzureStorageBlobs")]
        public void Test_AzureStorageBlobs_HappyPath()
        {
            var id = Guid.NewGuid().ToString();
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithConnectionString(AzureBlobCnnString)
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var efEvent = new AuditEvent()
            {
                EventType = id,
                Environment = new AuditEventEnvironment()
                {
                    MachineName = "Machine",
                    UserName = "User"
                }
            };

            var blobName = dp.InsertEvent(efEvent);
            var efEventGet = dp.GetEvent<AuditEvent>(containerName, blobName.ToString());

            Assert.AreEqual(id, efEventGet.EventType);
            Assert.AreEqual("Machine", efEventGet.Environment.MachineName);
        }

        [Test]
        [Category("AzureStorageBlobs")]
        public async Task Test_AzureStorageBlobs_HappyPathAsync()
        {
            var id = Guid.NewGuid().ToString();
            var originalId = id;
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithConnectionString(AzureBlobCnnString)
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var efEvent = new AuditEvent()
            {
                EventType = id,
                Environment = new AuditEventEnvironment()
                {
                    MachineName = "Machine",
                    UserName = "User"
                }
            };

            var blobName = await dp.InsertEventAsync(efEvent);
            var efEventGet = await dp.GetEventAsync<AuditEvent>(containerName, blobName.ToString());

            Assert.AreEqual(id, efEventGet.EventType);
            Assert.AreEqual("Machine", efEventGet.Environment.MachineName);
        }

        [Test]
        [Category("AzureStorageBlobs")]
        public void Test_AzureStorageBlobs_ConnectionString()
        {
            var id = Guid.NewGuid().ToString();
            var originalId = id;
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithConnectionString(AzureBlobCnnString)
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Configuration.DataProvider = dp;

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
                    .Url(AzureBlobServiceUrl)
                    .Credential(new StorageSharedKeyCredential(AzureBlobAccountName, AzureBlobAccountKey)))
                .AccessTier(AccessTier.Cool)
                .BlobName(ev => ev.EventType + "_" + id + ".json")
                .ContainerName(ev => containerName)
                .Metadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }));

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Configuration.DataProvider = dp;

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
