using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.OpenAccess;

namespace Model
{
    public class ContextFactory
    {
        private const string ONLINE_STORAGE_CONNECTION_NAME = @"SQLServerConnection";
        private const string OFFLINE_STORAGE_CONNECTION_NAME = @"SQLiteConnection";

        private static BackendConfiguration onlineBackendConfig;
        private static BackendConfiguration offlineBackendConfig;

        private static FluentModelMetadataSource onlineMetadataSource;
        private static FluentModelMetadataSource offlineMetadataSource;

        private static ContextFactory factoryInstance = null;

        private ContextMode contextMode = ContextMode.OnlineMode;

        public static ContextFactory FactoryInstance
        {
            get
            {
                //Do note that this singleton implementation is not thread 
                //safe and this is fine in the context of the current sample.
                if (factoryInstance == null)
                {
                    onlineBackendConfig = new BackendConfiguration();
                    onlineBackendConfig.Backend = "MsSql";
                    onlineBackendConfig.ProviderName = "System.Data.SqlClient";

                    offlineBackendConfig = new BackendConfiguration();
                    offlineBackendConfig.Backend = "SQLite";
                    offlineBackendConfig.ProviderName = "System.Data.SQLite";

                    onlineMetadataSource = new FluentModelMetadataSource();
                    offlineMetadataSource = new FluentModelMetadataSource(true);

                    factoryInstance = new ContextFactory();
                }

                return factoryInstance;
            }
        }

        public ContextMode ContextMode
        {
            get
            {
                return this.contextMode;
            }
        }

        private ContextFactory()
        {
        }

        public FluentModel GetContext()
        {
            FluentModel context = null;

            if (this.contextMode == ContextMode.OnlineMode)
            {
                context = new FluentModel(ONLINE_STORAGE_CONNECTION_NAME, onlineBackendConfig, onlineMetadataSource, this.contextMode);
            }
            else
            {
                context = new FluentModel(OFFLINE_STORAGE_CONNECTION_NAME, offlineBackendConfig, offlineMetadataSource, this.contextMode);
            }

            return context;
        }

        public void SetContextMode(ContextMode contextMode)
        {
            this.contextMode = contextMode;
        }
    }
}
