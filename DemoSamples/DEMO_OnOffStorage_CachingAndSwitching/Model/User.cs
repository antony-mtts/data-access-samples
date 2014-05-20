using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Telerik.OpenAccess.SPI.dataobjects;

namespace Model
{
    [Serializable]
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public decimal Income { get; set; }
        public string Note { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }


        //<summary>
        //This method is required to serialize lazy loaded field values correctly.
        //</summary>
        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            ((PersistenceCapable)this).OpenAccessEnhancedPreSerialize();
        }
    }
}
