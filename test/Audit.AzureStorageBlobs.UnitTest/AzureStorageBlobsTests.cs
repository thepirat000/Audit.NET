using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using Audit.IntegrationTest;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using NUnit.Framework;

namespace Audit.AzureStorageBlobs.UnitTest
{
    public class AzureStorageBlobsTests
    {
        [Test]
        [Category("Integration")]
        [Category("Azure")]
        public void Test_AzureStorageBlobs_HappyPath()
        {
            var id = Guid.NewGuid().ToString();
            var containerName = $"events{DateTime.Today:yyyyMMdd}";
            var dp = new AzureStorageBlobs.Providers.AzureStorageBlobDataProvider(config => config
                .WithConnectionString(AzureSettings.AzureBlobCnnString)
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

            Assert.That(efEventGet.EventType, Is.EqualTo(id));
            Assert.That(efEventGet.Environment.MachineName, Is.EqualTo("Machine"));
        }

        [Test]
        [Category("Integration")]
        [Category("Azure")]
        public async Task Test_AzureStorageBlobs_HappyPathAsync()
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

            Assert.That(efEventGet.EventType, Is.EqualTo(id));
            Assert.That(efEventGet.Environment.MachineName, Is.EqualTo("Machine"));
        }

        [Test]
        [Category("Integration")]
        [Category("Azure")]
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

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Configuration.DataProvider = dp;

            using (var scope = AuditScope.Create("Test", () => id, new { custom = 123 }))
            {
                id = "changed!";
            }

            var result = dp.GetEvent<AuditEvent>(containerName, "Test" + "_" + originalId + ".json");
            var result2 = dp.GetEvent<AuditEvent>(containerName, "NotExists.json");

            Assert.That(result, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(result.CustomFields["custom"].ToString(), Is.EqualTo("123"));
            Assert.That(result.Target.New.ToString(), Is.EqualTo("changed!"));
            Assert.That(result.Target.Old.ToString(), Is.EqualTo(originalId));
            Assert.That(result.EventType, Is.EqualTo("Test"));
        }

        [Test]
        [Category("Integration")]
        [Category("Azure")]
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

            Configuration.ResetCustomActions();
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            Configuration.DataProvider = dp;

            using (var scope = AuditScope.Create("Test", () => id, new { custom = 123 }))
            {
                id = "changed!";
            }

            var result = dp.GetEvent<AuditEvent>(containerName, "Test" + "_" + originalId + ".json");
            var result2 = dp.GetEvent<AuditEvent>(containerName, "NotExists.json");

            Assert.That(result, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(result.CustomFields["custom"].ToString(), Is.EqualTo("123"));
            Assert.That(result.Target.New.ToString(), Is.EqualTo("changed!"));
            Assert.That(result.Target.Old.ToString(), Is.EqualTo(originalId));
            Assert.That(result.EventType, Is.EqualTo("Test"));
        }

    }
}
