using System;
using System.Collections.Generic;
using System.Linq;
using Audit.AzureDocumentDB.Providers;
using Audit.Core;
using Audit.Core.Providers;
using Audit.MongoDB.Providers;
using Audit.SqlServer.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audit.UnitTest
{
    public static class Program
    {
        public static void Main()
        {
            var t = new AuditTests();
            t.TestUpdate();
            t.TestInsert();
            t.TestDelete();
        }
    }

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

        public struct Fede
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
            using (var a = AuditScope.Create(eventType, () => new Fede { Id = 123, Order = order }, order.OrderId))
            {
                a.SetCustomField("TestGuid", Guid.NewGuid());
                order = DbOrderUpdateStatus(order, CustomerOrderStatusType.Submitted);
            }

            order = DbCreateOrder();

            //audit multiple 
            using (AuditScope.Create(eventType, () => new { OrderStatus = order.Status, Items = order.OrderItems }, order.OrderId))
            {
                order = DbOrderUpdateStatus(order, CustomerOrderStatusType.Submitted);
            }

            order = DbCreateOrder();
            

            using (var audit = AuditScope.Create("Order:Update", () => order.Status, order.OrderId))
            {
                audit.SetCustomField("Reason", reasonText);
                audit.SetCustomField("ItemsBefore", order.OrderItems);
                audit.SetCustomField("FirstItem", order.OrderItems.FirstOrDefault());

                order = DbOrderUpdateStatus(order, CustomerOrderStatusType.Submitted);
                audit.SetCustomField("ItemsAfter", order.OrderItems);
                audit.Comment("Status Updated to Submitted");
                audit.Comment("Another Comment");
            }

            order = DbCreateOrder();

            using (var audit = new AuditScope<CustomerOrder>(eventType, () => order, order.OrderId))
            {
                audit.SetCustomField("Reason", "reason");
                ExecuteStoredProcedure(order, CustomerOrderStatusType.Submitted);
                order.Status = CustomerOrderStatusType.Submitted;
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
                audit.ReferenceId = order.OrderId;
            }
        }

        public void TestDelete()
        {
            CustomerOrder order = DbCreateOrder();

            using (var audit = AuditScope.Create("Order:Delete", () => order, order.OrderId))
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
                DirectoryPath = @"c:\temp\1"
            });
        }

        public void SetAzureSettings()
        {
            AuditConfiguration.SetDataProvider(new AzureDbDataProvider()
            {
                ConnectionString = "https://thepirat.documents.azure.com:443/",
                AuthKey = "dQMaxMIXj2Ma2smfbFsJpyx1T6O62dGmsWPZ7W7hnTRFgZy78pe411a6ldlabMBzzbt7Iugpx7t7ovz3XDfuzQ==",
                Database = "Audit",
                Collection = "Event"
            });
        }
        public void SetSqlSettings()
        {
            AuditConfiguration.SetDataProvider(new SqlDataProvider()
            {
                ConnectionString =
                    "data source=a4zgwwmufy.database.windows.net;initial catalog=Audit;user id=sqlguy;password=Herb1234",
                TableName = "Audit"
            });
        }
        public void SetMongoSettings()
        {
            AuditConfiguration.SetDataProvider(new MongoDataProvider()
            {
                ConnectionString = "mongodb://thepirat-win.cloudapp.net:27017",
                Database = "Audit",
                Collection = "Event"
            });
        }

        public static void ExecuteStoredProcedure(CustomerOrder order, CustomerOrderStatusType status)
        {
        }

        public static void DbDeteleOrder(string id)
        {
        }

        public static CustomerOrder DbCreateOrder()
        {
            var order = new CustomerOrder()
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = "customer 123 some 'quotes' to test's. double ''. some double \"quotes\" \"",
                DistributorId = "STAFF",
                Status = CustomerOrderStatusType.Created,
                OrderItems = new List<CustomerOrderItem>()
                {
                    new CustomerOrderItem()
                    {
                        Sku = "1002",
                        Quantity = 3
                    }
                }
            };
            return order;
        }
        public static CustomerOrder DbOrderUpdateStatus(CustomerOrder order, CustomerOrderStatusType newStatus)
        {
            order.Status = newStatus;
            order.OrderItems = null;
            return order;
        }
    }

    public class CustomerOrder
    {
        public string OrderId { get; set; }
        public CustomerOrderStatusType Status { get; set; }
        public string CustomerId { get; set; }
        public string DistributorId { get; set; }
        public IEnumerable<CustomerOrderItem> OrderItems { get; set; }
    }

    public class CustomerOrderItem
    {
        public string Sku { get; set; }
        public double Quantity { get; set; }
    }

    public enum CustomerOrderStatusType
    {
        Created = 2,
        Submitted = 4,
        Cancelled = 10
    }
}
