using System;
using System.Runtime.Serialization;

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

        public ProductCustomized()
        {
        }

        protected ProductCustomized(SerializationInfo info, StreamingContext context)
        {
            this.ProductID = info.GetInt32("ProductID");
            this.ProductName = info.GetString("ProductName");
            this.CategoryName = (string)info.GetValue("CategoryName", typeof(string));
            this.QuantityPerUnit = info.GetString("QuantityPerUnit");
            this.UnitPrice = (decimal?)info.GetValue("UnitPrice", typeof(decimal?));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ProductID", this.ProductID, typeof(int));
            info.AddValue("ProductName", this.ProductName, typeof(string));
            info.AddValue("CategoryName", this.CategoryName, typeof(string));
            info.AddValue("QuantityPerUnit", this.QuantityPerUnit, typeof(string));
            info.AddValue("UnitPrice", this.UnitPrice, typeof(decimal?));
        }
    }
}
