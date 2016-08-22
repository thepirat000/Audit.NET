using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audit.AzureDocumentDB.Providers;
using Audit.Core;
using Audit.Core.Providers;
using Audit.MongoDB.Providers;
using Audit.SqlServer.Providers;
using Microsoft.VisualBasic.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audit.UnitTest
{
    public class IntegrationTests
    {
        [TestClass]
        public class AuditTests
        {
            [TestMethod]
            public void TestEventLog()
            {
                SetEventLogSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [TestMethod]
            public void TestFile()
            {
                SetFileSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [TestMethod]
            public void TestAzure()
            {
                SetAzureSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [TestMethod]
            public void TestSql()
            {
                SetSqlSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [TestMethod]
            public void TestMongo()
            {
                SetMongoSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            public struct TestStruct
            {
                public int Id { get; set; }
                public CustomerOrder Order { get; set; }
            }

            public void TestUpdate()
            {
                var order = DbCreateOrder();
                var reasonText = "the order was updated because ...";
                var eventType = "Order:Update";

                //struct
                using (var a = AuditScope.Create(eventType, () => new TestStruct() { Id = 123, Order = order },
                       new { ReferenceId = order.OrderId }))
                {
                    a.SetCustomField("TestGuid", Guid.NewGuid());
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                order = DbCreateOrder();

                //audit multiple 
                using (AuditScope.Create(eventType, () => new { OrderStatus = order.Status, Items = order.OrderItems },
                    new { ReferenceId = order.OrderId }))
                {
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                order = DbCreateOrder();


                using (var audit = AuditScope.Create("Order:Update", () => order.Status, new { ReferenceId = order.OrderId }))
                {
                    audit.SetCustomField("Reason", reasonText);
                    audit.SetCustomField("ItemsBefore", order.OrderItems);
                    audit.SetCustomField("FirstItem", order.OrderItems.FirstOrDefault());

                    order = DbOrderUpdateStatus(order, IntegrationTests.OrderStatus.Submitted);
                    audit.SetCustomField("ItemsAfter", order.OrderItems);
                    audit.Comment("Status Updated to Submitted");
                    audit.Comment("Another Comment");
                }

                order = DbCreateOrder();

                using (var audit = new AuditScope(eventType, () => order, new { ReferenceId = order.OrderId }))
                {
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                order = DbCreateOrder();
            }

            public void TestInsert()
            {
                CustomerOrder order = null;
                using (var audit = AuditScope.Create("Order:Create", () => order))
                {
                    order = DbCreateOrder();
                    audit.SetCustomField("ReferenceId", order.OrderId);
                }
            }

            public void TestDelete()
            {
                IntegrationTests.CustomerOrder order = DbCreateOrder();

                using (var audit = AuditScope.Create("Order:Delete", () => order, new { ReferenceId = order.OrderId }))
                {
                    DbDeteleOrder(order.OrderId);
                    order = null;
                }
            }

            public void SetEventLogSettings()
            {
                AuditConfiguration.SetDataProvider(new EventLogDataProvider()
                {
                    SourcePath = "Application",
                    LogName = "Application",
                    MachineName = "."
                });
            }

            public void SetFileSettings()
            {
                AuditConfiguration.SetDataProvider(new FileDataProvider()
                {
                    FilenamePrefix = "Event_",
                    DirectoryPath = @"c:\temp\1",
                    CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
                });
            }

            public void SetAzureSettings()
            {
                AuditConfiguration.SetDataProvider(new AzureDbDataProvider()
                {
                    ConnectionString = "https://thepirat.documents.azure.com:443/",
                    AuthKey = "xxxxxxxx",
                    Database = "Audit",
                    Collection = "Event",
                    CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
                });
            }

            public void SetSqlSettings()
            {
                AuditConfiguration.SetDataProvider(new SqlDataProvider()
                {
                    ConnectionString =
                        "data source=localhost;initial catalog=Audit;integrated security=true;",
                    TableName = "Event",
                    JsonColumnName = "Data",
                    IdColumnName = "EventId",
                    LastUpdatedDateColumnName = "LastUpdatedDate",
                    CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd
                });
            }

            public void SetMongoSettings()
            {
                AuditConfiguration.SetDataProvider(new MongoDataProvider()
                {
                    ConnectionString = "mongodb://localhost:27017",
                    Database = "Audit",
                    Collection = "Event",
                    CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
                });
            }

            public static void ExecuteStoredProcedure(IntegrationTests.CustomerOrder order, IntegrationTests.OrderStatus status)
            {
            }

            public static void DbDeteleOrder(string id)
            {
            }

            public static IntegrationTests.CustomerOrder DbCreateOrder()
            {
                var order = new IntegrationTests.CustomerOrder()
                {
                    OrderId = Guid.NewGuid().ToString(),
                    CustomerId = "customer 123 some 'quotes' to test's. double ''. some double \"quotes\" \"",
                    Status = IntegrationTests.OrderStatus.Created,
                    OrderItems = new List<IntegrationTests.CustomerOrderItem>()
                    {
                        new IntegrationTests.CustomerOrderItem()
                        {
                            Sku = "1002",
                            Quantity = 3
                        }
                    }
                };
                return order;
            }

            public static IntegrationTests.CustomerOrder DbOrderUpdateStatus(IntegrationTests.CustomerOrder order,
                IntegrationTests.OrderStatus newStatus)
            {
                order.Status = newStatus;
                order.OrderItems = null;
                return order;
            }
        }

        public class CustomerOrder
        {
            public string OrderId { get; set; }
            public IntegrationTests.OrderStatus Status { get; set; }
            public string CustomerId { get; set; }
            public IEnumerable<IntegrationTests.CustomerOrderItem> OrderItems { get; set; }
        }

        public class CustomerOrderItem
        {
            public string Sku { get; set; }
            public double Quantity { get; set; }
        }

        public enum OrderStatus
        {
            Created = 2,
            Submitted = 4,
            Cancelled = 10
        }
    }
}