using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using Telerik.OpenAccess;
using Telerik.OpenAccess.FetchOptimization;
using System.Threading;
using Telerik.OpenAccess.Exceptions;

namespace Model
{
    public class SmartContext : IDisposable
    {
        private FluentModel primaryContext;
        private FluentModel backupContext;
        private ContextFactory contextFactory;

        public SmartContext()
        {
            this.contextFactory = ContextFactory.FactoryInstance;

            this.contextFactory.SetContextMode(ContextMode.OfflineMode);
            this.backupContext = this.contextFactory.GetContext();

            this.contextFactory.SetContextMode(ContextMode.OnlineMode);
            this.primaryContext = this.contextFactory.GetContext();
        }

        public IEnumerable<T> GetMultiple<T>(Expression<Func<T, bool>> condition = null, FetchStrategy fetchStrategy = null)
        {
            this.primaryContext.FetchStrategy = fetchStrategy;

            var query = this.primaryContext.GetAll<T>();

            if (condition != null)
            {
                query = query.Where(condition);
            }

            IList<T> entities = entities = query.ToList();

            this.primaryContext.FetchStrategy = null; //reset fetch strategy

            //backup data retrieved from the online storage
            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                this.SaveToBackupStorage<T>(entities, fetchStrategy);
            }

            return entities;
        }

        public T GetSingle<T>(Expression<Func<T, bool>> condition = null, FetchStrategy fetchStrategy = null)
        {
            this.primaryContext.FetchStrategy = fetchStrategy;

            var query = this.primaryContext.GetAll<T>();

            if (condition != null)
            {
                query = query.Where(condition);
            }

            T entity = query.FirstOrDefault();

            this.primaryContext.FetchStrategy = null; //reset fetch strategy

            //backup data retrieved from the online storage
            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                this.SaveToBackupStorage<T>(entity, fetchStrategy);
            }

            return entity;
        }

        public void Add(Object entity)
        {
            this.primaryContext.Add(entity);
        }

        public void Delete(Object entity)
        {
            //delete the entity from the backup storage and backup the delete operation
            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                object detachedEntityToDelete = this.primaryContext.CreateDetachedCopy(entity);
                object entityClone = DeepClone(entity);
                object attachedObject = this.backupContext.AttachCopy(entityClone);
                this.backupContext.Delete(attachedObject);

                this.BackupDeleteOperation(entity);
            }

            this.primaryContext.Delete(entity);
        }

        public void SaveChanges()
        {
            if (this.primaryContext.HasChanges)
            {
                //backup the changes that are going to be pushed to the online storage
                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    //get the changes from the online context
                    ContextChanges contextChanges = this.primaryContext.GetChanges();

                    IList<User> userInserts = contextChanges.GetInserts<User>();
                    IList<User> userUpdates = contextChanges.GetUpdates<User>();

                    IList<Group> groupInserts = contextChanges.GetInserts<Group>();
                    IList<Group> groupUpdates = contextChanges.GetUpdates<Group>();

                    //apply the changes from the online context to the offline context
                    this.SaveToBackupStorage<User>(userInserts);
                    this.SaveToBackupStorage<User>(userUpdates);
                    this.SaveToBackupStorage<Group>(groupInserts);
                    this.SaveToBackupStorage<Group>(groupUpdates);
                }

                this.primaryContext.SaveChanges();
            }

            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                this.backupContext.SaveChanges();
            }
        }

        public void ClearChanges()
        {
            this.primaryContext.ClearChanges();

            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                this.backupContext.ClearChanges();
            }
        }

        private void SaveToBackupStorage<T>(IEnumerable<T> entities, FetchStrategy fetchStrategy = null)
        {
            foreach (T entity in entities)
            {
                //special handling for New entities (inserts) as they cannot be detached and need to only be copied
                if (OpenAccessContext.PersistenceState.GetState(entity) == ObjectState.New)
                {
                    T clone = DeepClone<T>(entity);
                    this.backupContext.AttachCopy<T>(clone);
                }
                else
                {
                    this.AttachToBackupContext<T>(entity, fetchStrategy);
                }

            }

            this.backupContext.SaveChanges();
        }

        private void SaveToBackupStorage<T>(T entity, FetchStrategy fetchStrategy = null)
        {
            this.AttachToBackupContext<T>(entity, fetchStrategy);
            this.backupContext.SaveChanges();
        }

        private void AttachToBackupContext<T>(T entity, FetchStrategy fetchStrategy = null)
        {
            T detachedEntity = this.primaryContext.CreateDetachedCopy(entity, fetchStrategy);
            T cloneEntity = DeepClone<T>(detachedEntity);
            this.backupContext.AttachCopy(cloneEntity);
        }

        private void BackupDeleteOperation<T>(T objectToDelte)
        {
            ObjectKey objectToDeleteObjectKey = this.primaryContext.CreateObjectKey(objectToDelte);
            //get the identity
            int entityToDeleteId = (int)objectToDeleteObjectKey.ObjectKeyValues.FirstOrDefault().Value;
            //get the type
            string entityToDeleteType = objectToDeleteObjectKey.TypeName.Split('.')[1];

            //backup the delete operation
            object entityToDelete = this.backupContext.CreateInstance(DeleteOperationDefinition.DeleteOperationFullTypeName);
            this.backupContext.Add(entityToDelete);
            entityToDelete.SetFieldValue(DeleteOperationDefinition.EntityToDeleteId, entityToDeleteId);
            entityToDelete.SetFieldValue(DeleteOperationDefinition.EntityToDeleteType, entityToDeleteType);
        }

        private static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                T clone = (T)formatter.Deserialize(ms);
                return clone;
            }
        }

        public void Dispose()
        {
            if (this.primaryContext != null)
            {
                this.primaryContext.DisposeDatabase();
                this.primaryContext.Dispose();
            }

            if (this.backupContext != null)
            {
                this.backupContext.DisposeDatabase();
                this.backupContext.Dispose();
            }
        }
    }
}
