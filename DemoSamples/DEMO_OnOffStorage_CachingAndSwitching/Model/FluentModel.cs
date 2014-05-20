using System.Linq;
using Telerik.OpenAccess;
using Telerik.OpenAccess.Metadata;

namespace Model	
{
	public partial class FluentModel : OpenAccessContext
	{
		//private static MetadataSource metadataSource = new FluentModelMetadataSource();

        private IObjectScope objectScope;

        //custom field indicating the mode of the context
        private ContextMode mode;
	

        //custom constructor to initialize the context with specific connection string and backend configuration
        public FluentModel(string connectionStringName, BackendConfiguration backendConfiguration, FluentModelMetadataSource metadataSource, ContextMode contextMode)
            : base(connectionStringName, backendConfiguration, metadataSource)
        {
            this.mode = contextMode;
        }

        //custom property allowing to check whether the context instance is using the online database
        public ContextMode Mode
        {
            get
            {
                return this.mode;
            }
        }

        //only used to set the CommandTimeout for the purpose of ContextSwitchTests
        public Telerik.OpenAccess.BackendConfiguration BackendConfiguration
        {
            get
            {
                if (this.objectScope == null)
                {
                    this.objectScope = this.GetScope();
                }
                return this.objectScope.Database.BackendConfiguration;
            }
        }

        public IQueryable<User> Users
        {
            get
            {
                return this.GetAll<User>();
            }
        }

        public IQueryable<Group> Groups
        {
            get
            {
                return this.GetAll<Group>();
            }
        }

        //creates or updates the database schema based on the current model
        public void CreateUpdateDatabase()
        {
            ISchemaHandler handler = this.GetSchemaHandler();

            string ddlScript = null;

            if (handler.DatabaseExists())
            {
                ddlScript = handler.CreateUpdateDDLScript(null);
            }
            else
            {
                handler.CreateDatabase();
                ddlScript = handler.CreateDDLScript();
            }

            if (string.IsNullOrEmpty(ddlScript) == false)
            {
                handler.ExecuteDDLScript(ddlScript);
            }
        }
		
		/// <summary>
		/// Allows you to customize the BackendConfiguration of FluentModel.
		/// </summary>
		/// <param name="config">The BackendConfiguration of FluentModel.</param>
		static partial void CustomizeBackendConfiguration(ref BackendConfiguration config);
		
	}
}
