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
using System.Collections;

namespace Model
{
    public class SmartContext : IDisposable
    {
        private FluentModel primaryContext;
        private FluentModel backupContext;
        private ContextFactory contextFactory;
        private Timer connectionChecker;
        private Func<Exception, bool> isConnectionUnavailableEvaluator;
        private Action<OpenAccessContext, OpenAccessContext> offlineToOnlineSynchronizer;
        private Object operationsLock = new Object();

        public SmartContext()
            : this(DefaultConnectionEvaluator, DefaultOfflineToOnlineSynchronizer)
        {
        }

        public SmartContext(Func<Exception, bool> connectionEvaluator, Action<OpenAccessContext, OpenAccessContext> offlineToOnlineSynchronizer)
        {
            this.contextFactory = ContextFactory.FactoryInstance;

            this.contextFactory.SetContextMode(ContextMode.OfflineMode);
            this.backupContext = this.contextFactory.GetContext();

            this.contextFactory.SetContextMode(ContextMode.OnlineMode);
            this.primaryContext = this.contextFactory.GetContext();

            this.isConnectionUnavailableEvaluator = connectionEvaluator;
            this.offlineToOnlineSynchronizer = offlineToOnlineSynchronizer;
        }

        public IEnumerable<T> GetMultiple<T>(Expression<Func<T, bool>> condition = null, FetchStrategy fetchStrategy = null)
        {
            lock (this.operationsLock)
            {
                this.primaryContext.FetchStrategy = fetchStrategy;

                var query = this.primaryContext.GetAll<T>();

                if (condition != null)
                {
                    query = query.Where(condition);
                }

                IList<T> entities = null;
                try
                {
                    entities = query.ToList();
                }
                catch (Exception ex)
                {
                    if (this.isConnectionUnavailableEvaluator(ex) && this.primaryContext.Mode == ContextMode.OnlineMode)
                    {
                        //primary storage unreachable - switch to backup
                        this.SwitchToOfflineStorage();
                        //recall the method
                        return this.GetMultiple<T>(condition, fetchStrategy);
                    }
                    else
                    {
                        throw;
                    }
                }

                this.primaryContext.FetchStrategy = null; //reset fetch strategy

                //backup data retrieved from the online storage
                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    this.SaveToBackupStorage<T>(entities, fetchStrategy);
                }

                return entities;
            }
        }

        public T GetSingle<T>(Expression<Func<T, bool>> condition = null, FetchStrategy fetchStrategy = null)
        {
            lock (this.operationsLock)
            {
                this.primaryContext.FetchStrategy = fetchStrategy;

                var query = this.primaryContext.GetAll<T>();

                if (condition != null)
                {
                    query = query.Where(condition);
                }

                T entity = default(T);

                try
                {
                    entity = query.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    if (this.isConnectionUnavailableEvaluator(ex) && this.primaryContext.Mode == ContextMode.OnlineMode)
                    {
                        //primary storage is unavailable - switch to backup
                        this.SwitchToOfflineStorage();
                        //recall the method
                        return this.GetSingle<T>(condition, fetchStrategy);
                    }
                    else
                    {
                        throw;
                    }
                }

                this.primaryContext.FetchStrategy = null; //reset fetch strategy

                //backup data retrieved from the online storage
                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    this.SaveToBackupStorage<T>(entity, fetchStrategy);
                }

                return entity;
            }
        }

        public void Add(Object entity)
        {
            lock (this.operationsLock)
            {
                this.primaryContext.Add(entity);
            }
        }

        public void Delete(Object entity)
        {
            lock (this.operationsLock)
            {
                //delete from backup storage
                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    //exception on debug sometimes (when attaching the detached object directly
                    object detachedEntityToDelete = this.primaryContext.CreateDetachedCopy(entity);
                    object entityClone = DeepClone(entity);
                    object attachedObject = this.backupContext.AttachCopy(entityClone);
                    this.backupContext.Delete(attachedObject);
                }

                //always backup delete operation
                this.BackupDeleteOperation(entity);

                try
                {
                    this.primaryContext.Delete(entity);
                }
                catch (Exception ex)
                {
                    if (this.isConnectionUnavailableEvaluator(ex) && this.primaryContext.Mode == ContextMode.OnlineMode)
                    {
                        this.SwitchToOfflineStorage();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public void SaveChanges()
        {
            lock (this.operationsLock)
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
                        IList<User> userDeletes = contextChanges.GetDeletes<User>();

                        IList<Group> groupInserts = contextChanges.GetInserts<Group>();
                        IList<Group> groupUpdates = contextChanges.GetUpdates<Group>();
                        IList<Group> groupDeletes = contextChanges.GetDeletes<Group>();

                        //apply the changes from the online context to the offline context
                        this.SaveToBackupStorage<User>(userInserts);
                        this.SaveToBackupStorage<User>(userUpdates);
                        this.SaveToBackupStorage<Group>(groupInserts);
                        this.SaveToBackupStorage<Group>(groupUpdates);
                    }

                    try
                    {
                        this.primaryContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        if (this.isConnectionUnavailableEvaluator(ex) && this.primaryContext.Mode == ContextMode.OnlineMode)
                        {
                            this.SwitchToOfflineStorage();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    this.backupContext.SaveChanges();
                }
            }
        }

        public void ClearChanges()
        {
            lock (this.operationsLock)
            {
                this.primaryContext.ClearChanges();

                if (this.primaryContext.Mode == ContextMode.OnlineMode)
                {
                    this.backupContext.ClearChanges();
                }
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
            string entityToDeleteType = objectToDeleteObjectKey.TypeName;

            //backup the delete operation
            object entityToDelete = null;

            FluentModel contextInUse = null;
            if (this.primaryContext.Mode == ContextMode.OnlineMode)
            {
                contextInUse = this.backupContext;
            }
            else
            {
                contextInUse = this.primaryContext;
            }

            entityToDelete = contextInUse.CreateInstance(DeleteOperationDefinition.DeleteOperationFullTypeName);

            contextInUse.Add(entityToDelete);
            entityToDelete.SetFieldValue(DeleteOperationDefinition.EntityToDeleteId, entityToDeleteId);
            entityToDelete.SetFieldValue(DeleteOperationDefinition.EntityToDeleteType, entityToDeleteType);
        }

        private void SwitchToOfflineStorage()
        {
            this.primaryContext.Dispose();
            this.primaryContext = this.backupContext;
            this.backupContext = null;
            this.StartOnlineConnectionChecker();
        }

        private void StartOnlineConnectionChecker()
        {
            TimerCallback callback = new TimerCallback(this.CheckOnlineStorageAvailability);
            this.connectionChecker = new Timer(callback, null, 10000, 10000);
        }

        private void CheckOnlineStorageAvailability(object StateObj)
        {
            lock (this.operationsLock)
            {
                contextFactory.SetContextMode(ContextMode.OnlineMode);
                using (FluentModel onlineContxt = contextFactory.GetContext())
                {
                    bool exceptionIsThrown = false;

                    try
                    {
                        User usr = onlineContxt.Users.FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        exceptionIsThrown = true;
                    }

                    if (exceptionIsThrown == false)
                    {
                        this.SwitchToOnlineStorage();
                    }
                }
            }
        }

        private void SwitchToOnlineStorage()
        {
            this.connectionChecker.Dispose();
            this.connectionChecker = null;

            this.contextFactory.SetContextMode(ContextMode.OnlineMode);
            FluentModel onlineContext = this.contextFactory.GetContext();
            this.backupContext = this.primaryContext;
            this.primaryContext = onlineContext;

            this.backupContext.SaveChanges();

            if (this.offlineToOnlineSynchronizer != null)
            {
                offlineToOnlineSynchronizer.Invoke(this.backupContext, this.primaryContext);
            }
        }

        private static bool DefaultConnectionEvaluator(Exception ex)
        {
            if (ex is LockNotGrantedException || ex is OptimisticVerificationException)
            {
                return true;
            }

            return false;
        }

        private static void DefaultOfflineToOnlineSynchronizer(OpenAccessContext backupContext, OpenAccessContext primaryContext)
        {
            //get the deleteOperations which need to be pushed to the primary storage
            var deleteOperations = backupContext.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName).Cast<object>().ToList();

            foreach (var deleteOperation in deleteOperations)
            {
                string entityToDeleteType = deleteOperation.FieldValue<string>(DeleteOperationDefinition.EntityToDeleteType);
                int entityToDeleteId = deleteOperation.FieldValue<int>(DeleteOperationDefinition.EntityToDeleteId);

                //GetObjectsByKeys Info:
                //http://docs.telerik.com/data-access/feature-reference/api/objectkey-api/data-access-tasks-object-key-api-obtain-objects-by-keys
                ObjectKey entityToDeleteObjectKey = new ObjectKey(entityToDeleteType, entityToDeleteId);

                object entityToDelete = null;
                if (primaryContext.TryGetObjectByKey(entityToDeleteObjectKey, out entityToDelete))
                {
                    primaryContext.Delete(entityToDelete);
                }
            }

            primaryContext.SaveChanges();

            //clear the stored DeleteOperation objects as they are already pushed to the primary storage
            //and therefore no longer needed
            backupContext.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName).DeleteAll();
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
            lock (this.operationsLock)
            {
                if (this.connectionChecker != null)
                {
                    this.connectionChecker.Dispose();
                }

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
}
