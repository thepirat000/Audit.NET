using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Audit.Wcf.UnitTest
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CatalogService" in both code and config file together.
    public class CatalogService : ICatalogService
    {
        public ProductResponse InsertProduct(string id, Product product)
        {
            if (product.Price < 0)
            {
                throw new ArgumentException("wrong price");
            }
            return new ProductResponse()
            {
                Success = true,
                Product = product
            };
        }
    }
}
