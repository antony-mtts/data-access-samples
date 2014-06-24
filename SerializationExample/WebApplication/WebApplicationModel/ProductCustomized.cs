using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationModel
{
    [Serializable]
    public partial class ProductCustomized: Product
    {
        private string categoryName;
        public string CategoryName 
        { 
            get 
            {
                return this.categoryName;
            } 
            set 
            {
                this.categoryName = value;
            } 
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("ProductID", this.ProductID, typeof(int));
            info.AddValue("ProductName", this.ProductName, typeof(string));
            info.AddValue("CategoryName", this.CategoryName, typeof(string));
            info.AddValue("QuantityPerUnit", this.QuantityPerUnit, typeof(string));
            info.AddValue("UnitPrice", this.UnitPrice, typeof(decimal?));
        }
    }
}
