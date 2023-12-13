using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Audit.Wcf.UnitTest
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICatalogService" in both code and config file together.
    [ServiceContract]
    public interface ICatalogService
    {
        [OperationContract]
        ProductResponse InsertProduct(string id, Product product);
    }

    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class ProductResponse
    {
        public bool Success { get; set; }
        public Product Product { get; set; }
    }
}
