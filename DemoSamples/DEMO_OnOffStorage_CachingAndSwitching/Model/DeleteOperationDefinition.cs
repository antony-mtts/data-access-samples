using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public static class DeleteOperationDefinition
    {
        private const string DELETE_OPERATION_TYPE_NAMESPACE = "Model";
        private const string DELETE_OPERATION_TYPE_NAME = "DeleteOperation";
        private const string DELETE_OPERATION_ID = "DeleteOperationId";
        private const string ENTITY_TO_DELETE_ID = "EntityToDeleteId";
        private const string ENTITY_TO_DELETE_TYPE = "EntityToDeleteType";

        public static string DeleteOperationTypeNamespace
        {
            get
            {
                return DELETE_OPERATION_TYPE_NAMESPACE;
            }
        }

        public static string DeleteOperationTypeName
        {
            get
            {
                return DELETE_OPERATION_TYPE_NAME;
            }
        }

        public static string DeleteOperationFullTypeName
        {
            get
            {
                string fullTypeName = string.Format("{0}.{1}", DELETE_OPERATION_TYPE_NAMESPACE, DELETE_OPERATION_TYPE_NAME);
                return fullTypeName;
            }
        }

        public static string DeleteOperationId
        {
            get
            {
                return DELETE_OPERATION_ID;
            }
        }

        public static string EntityToDeleteId
        {
            get
            {
                return ENTITY_TO_DELETE_ID;
            }
        }

        public static string EntityToDeleteType
        {
            get
            {
                return ENTITY_TO_DELETE_TYPE;
            }
        }
    }
}
