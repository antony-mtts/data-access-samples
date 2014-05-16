using System.Collections.Generic;
using Telerik.OpenAccess;
using Telerik.OpenAccess.Metadata;
using Telerik.OpenAccess.Metadata.Fluent;
using Telerik.OpenAccess.Metadata.Fluent.Advanced;
using Telerik.OpenAccess.Metadata.Fluent.Artificial;

namespace Model
{
	public partial class FluentModelMetadataSource : FluentMetadataSource
	{
        private bool shouldHaveDeletesTable;

        public FluentModelMetadataSource()
            :this(false)
        {
        }

        public FluentModelMetadataSource(bool shouldHaveDeletesTable)
        {
            this.shouldHaveDeletesTable = shouldHaveDeletesTable;
        }

		protected override IList<MappingConfiguration> PrepareMapping()
		{
			List<MappingConfiguration> mappingConfigurations = new List<MappingConfiguration>();

            mappingConfigurations.Add(this.PrepareGroupMappingConfig());
            mappingConfigurations.Add(this.PrepareUserMappingConfig());

            //the DeleteOperation configuration should only be added
            //for the offline context
            if (this.shouldHaveDeletesTable)
            {
                mappingConfigurations.Add(this.PrepareEntityDeleteConfig());
            }
			
			return mappingConfigurations;
		}

        private MappingConfiguration<User> PrepareUserMappingConfig()
        {
            MappingConfiguration<User> userMappingConfig = new MappingConfiguration<User>();

            userMappingConfig.MapType().ToTable("Users");
            
            //backend independent property mapping
            userMappingConfig.HasProperty(usr => usr.UserId).IsIdentity().WithOpenAccessType(OpenAccessType.Int32).ToColumn("UserId");
            userMappingConfig.HasProperty(usr => usr.Name).WithOpenAccessType(OpenAccessType.StringInfiniteLength).ToColumn("Name");
            userMappingConfig.HasProperty(usr => usr.Email).WithOpenAccessType(OpenAccessType.StringInfiniteLength).ToColumn("Email");
            userMappingConfig.HasProperty(usr => usr.Note).WithOpenAccessType(OpenAccessType.StringInfiniteLength).ToColumn("Note");
            userMappingConfig.HasProperty(usr => usr.Income).WithOpenAccessType(OpenAccessType.Decimal).ToColumn("Income").HasPrecision(12).HasScale(2);
            userMappingConfig.HasProperty(usr => usr.GroupId).WithOpenAccessType(OpenAccessType.Int32).ToColumn("GroupId");

            return userMappingConfig;
        }

        private MappingConfiguration<Group> PrepareGroupMappingConfig()
        {
            MappingConfiguration<Group> groupMappingConfig = new MappingConfiguration<Group>();

            groupMappingConfig.MapType().ToTable("Groups");

            //backend independent property mapping
            groupMappingConfig.HasProperty(grp => grp.GroupId).IsIdentity().WithOpenAccessType(OpenAccessType.Int32).ToColumn("GroupId");
            groupMappingConfig.HasProperty(grp => grp.Name).WithOpenAccessType(OpenAccessType.StringInfiniteLength).ToColumn("Name");
            groupMappingConfig.HasProperty(grp => grp.Description).WithOpenAccessType(OpenAccessType.StringInfiniteLength).ToColumn("Description");

            groupMappingConfig.HasAssociation(grp => grp.UsersInGroup)
                .WithOpposite(usr => usr.Group)
                .HasConstraint((grp, usr) => grp.GroupId == usr.GroupId)
                .IsManaged();

            return groupMappingConfig;
        }

        //artificial type for backing up online deletes and "executing" offline deletes
        private MappingConfiguration PrepareEntityDeleteConfig()
        {
            MappingConfiguration delEntityConfig = new MappingConfiguration(DeleteOperationDefinition.DeleteOperationTypeName, DeleteOperationDefinition.DeleteOperationTypeNamespace);
            delEntityConfig.HasArtificialPrimitiveProperty<int>(DeleteOperationDefinition.DeleteOperationId).IsIdentity(KeyGenerator.Autoinc).ToColumn(DeleteOperationDefinition.DeleteOperationId);
            delEntityConfig.HasArtificialPrimitiveProperty<int>(DeleteOperationDefinition.EntityToDeleteId).ToColumn(DeleteOperationDefinition.EntityToDeleteId);
            delEntityConfig.HasArtificialPrimitiveProperty<string>(DeleteOperationDefinition.EntityToDeleteType).ToColumn(DeleteOperationDefinition.EntityToDeleteType);

            return delEntityConfig;
        }
		
		protected override void SetContainerSettings(MetadataContainer container)
		{
			container.Name = "FluentModel";
			container.DefaultNamespace = "Model";
			container.NameGenerator.SourceStrategy = Telerik.OpenAccess.Metadata.NamingSourceStrategy.Property;
			container.NameGenerator.RemoveCamelCase = false;
		}

        protected override MetadataContainer CreateModel()
        {
            MetadataContainer container = base.CreateModel();
            container.DefaultMapping.NullForeignKey = true;
            return container;
        }
	}
}
