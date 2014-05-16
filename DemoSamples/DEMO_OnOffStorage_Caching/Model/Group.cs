using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Telerik.OpenAccess.SPI.dataobjects;

namespace Model
{
    [Serializable]
    public class Group
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        private IList<User> usersInGroup = new List<User>();
        public IList<User> UsersInGroup
        {
            get
            {
                return this.usersInGroup;
            }
        }

        /// <summary>
        /// This method is required to serialize lazy loaded field values correctly.
        /// </summary>
        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
            ((PersistenceCapable)this).OpenAccessEnhancedPreSerialize();
        }
    }
}
