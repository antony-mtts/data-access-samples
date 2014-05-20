using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using Telerik.OpenAccess.Exceptions;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Telerik.OpenAccess;

namespace ContextTests
{
    [TestClass]
    public class ContextSwitchTests : BaseTestClass
    {
        //a context used to lock the Users table in order to simulate unavailable database server
        private FluentModel lockContext;
        private FluentModel onlineContext;
        private FluentModel offlineContext;

        [TestInitialize]
        public void ContextSwitchTeststInit()
        {
            //prepare the online storage
            base.BaseTestInit();

            //prepare the offline storage
            BaseTestClass.contextFactory.SetContextMode(ContextMode.OfflineMode);
            using (FluentModel context = BaseTestClass.contextFactory.GetContext())
            {
                BaseTestClass.EnsureDatabase(context);
                BaseTestClass.ClearDatabase(context);
                BaseTestClass.SeedDatabase(context);
                context.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName).DeleteAll();
            }

            BaseTestClass.contextFactory.SetContextMode(ContextMode.OnlineMode);
            this.lockContext = BaseTestClass.contextFactory.GetContext();
            this.onlineContext = BaseTestClass.contextFactory.GetContext();
            BaseTestClass.contextFactory.SetContextMode(ContextMode.OfflineMode);
            this.offlineContext = BaseTestClass.contextFactory.GetContext();
        }

        [TestCleanup]
        public void ContextSwitchTestsCleanup()
        {
            this.lockContext.DisposeDatabase();
            this.lockContext.Dispose();
            this.offlineContext.DisposeDatabase();
            this.offlineContext.Dispose();
        }

        [TestMethod]
        public void TestInsertNewUser_SwitchToOfflineContext_UserShouldBeInsertedInOfflineDb()
        {
            FluentModel context = BaseTestClass.contextFactory.GetContext();
            
            context.BackendConfiguration.Runtime.MediumTrust = true;
            context.BackendConfiguration.Runtime.CommandTimeout = 5;

            User newUsr = new User()
            {
                Name = "Harry Morgan",
                Note = "note note note",
                Email = "Harry@email.com",
                Income = 1111.123m
            };

            context.Add(newUsr);

            //lock the Users table to simulate unavailable database server
            this.LockDbTable("Users");

            try
            {
                //Save changes will fail because the table is being locked
                context.SaveChanges();
            }
            catch (LockNotGrantedException) //CommandTimeout expires assume that the server is not available
            {
                //switch to offline storage
                BaseTestClass.contextFactory.SetContextMode(ContextMode.OfflineMode);
                context = BaseTestClass.contextFactory.GetContext();

                context.Add(newUsr);
                context.SaveChanges();
            }
            finally
            {
                //release the table lock
                this.ReleaseLock();
            }

            //after being persisted we can access our new user object withut changes to the code
            //even tho we are using the offline storage
            User userFromLocalStorage = context.Users.FirstOrDefault(usr => usr.UserId == newUsr.UserId);
            Assert.IsNotNull(userFromLocalStorage);
            
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_SwitchOnRetrieval_ObjectsShouldBeRetrievedFromTheBackup()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                User offlineSpecificUser = new User()
                {
                    UserId = 8888,
                    Name = "Special Guy",
                    Email = "sg@mail.com",
                    Note = "note note note",
                    Income = 1234m
                };
                this.offlineContext.Add(offlineSpecificUser);
                this.offlineContext.SaveChanges();

                //Simulate that the online storage is not available
                this.LockDbTable("Users");

                //the SmartContext will now work with the backup storage and the required user will still be retrieved
                User userFromBackup = sCtx.GetSingle<User>(usr => usr.UserId == offlineSpecificUser.UserId);

                Assert.IsNotNull(userFromBackup);
            }
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_SwitchOnInsert_ObjectShouldBeInsertedInTheBackup()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineBefore = this.offlineContext.Users.Count();

                User newUser = new User()
                {
                    UserId = 7777,
                    Name = "Ivan",
                    Email = "Ivan@mail.com",
                    Note = "note note note",
                    Income = 15543m
                };

                //Simulate that the online storage is not available
                this.LockDbTable("Users");

                sCtx.Add(newUser);

                //Upon saving the changes, the SmartContext will switch to the backup storage and
                //persist the new user object there
                sCtx.SaveChanges();

                int usersInOfflineAfter = this.offlineContext.Users.Count();
                User newUserFromFromBackup = sCtx.GetSingle<User>(usr => usr.UserId == newUser.UserId);
                this.ReleaseLock();
                User newUserFromOnline = this.onlineContext.Users.FirstOrDefault(usr => usr.UserId == newUser.UserId);

                Assert.IsNotNull(newUserFromFromBackup);
                Assert.IsNull(newUserFromOnline);
                Assert.AreNotEqual(usersInOfflineBefore, usersInOfflineAfter);
                Assert.AreEqual(usersInOfflineBefore + 1, usersInOfflineAfter);
            }
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_SwitchOnUpdate_ObjecShouldBeUpdateInTheBackup()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                User userToUpdate = sCtx.GetSingle<User>();
                string newName = "Tony Stark";
                userToUpdate.Name = newName;

                //Simulate that the online storage is not available
                this.LockDbTable("Users");

                //Upon saving the changes the SmartContext will switch to the backup storage
                //and persist the changes made to the user there
                sCtx.SaveChanges();

                User updatedUserFromBackup = sCtx.GetSingle<User>(usr => usr.UserId == userToUpdate.UserId);
                this.ReleaseLock();
                User updatedUserFromOnline = this.onlineContext.Users.FirstOrDefault(usr => usr.UserId == userToUpdate.UserId);

                Assert.AreEqual(newName, updatedUserFromBackup.Name);
                Assert.AreNotEqual(newName, updatedUserFromOnline);
            }
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_SwitchOnDelete_ObjectShouldBeDeletedFromBackup()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                User userToDelete = sCtx.GetSingle<User>();
                int userToDeleteId = userToDelete.UserId;
                sCtx.Delete(userToDelete);

                //Simulate that the online storage is not available
                this.LockDbTable("Users");

                //Upon saving the chanegs the SmartContext will switch to the backup storage
                //and delete the user from there,
                //it will also save the delete operation
                sCtx.SaveChanges();

                User deletedUserFromBackup = sCtx.GetSingle<User>(usr => usr.UserId == userToDelete.UserId);
                this.ReleaseLock();
                User userFromOnline = this.onlineContext.Users.FirstOrDefault(usr => usr.UserId == userToDelete.UserId);

                object deleteOperation = this.offlineContext.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName)
                    .Cast<object>()
                    .FirstOrDefault(o => o.FieldValue<int>(DeleteOperationDefinition.EntityToDeleteId) == userToDeleteId);

                int deletedEntityId = deleteOperation.FieldValue<int>(DeleteOperationDefinition.EntityToDeleteId);
                string deletedEntityType = deleteOperation.FieldValue<string>((DeleteOperationDefinition.EntityToDeleteType));

                //Deleted from the backup storage
                Assert.IsNull(deletedUserFromBackup);
                //Still present in the online storage
                Assert.IsNotNull(userFromOnline);
                //verify that the delete operation has been backed up
                Assert.AreEqual(userToDeleteId, deletedEntityId);
                Assert.AreEqual("Model.User", deletedEntityType);
            }
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_SwitchBackToOnlineMode_ObjectShouldBeRetrievedFromTheOnlineStorage()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                //this is a user which is only available in the online storage
                User onlineSpecificUser = new User()
                {
                    UserId = 8888,
                    Name = "Special Guy",
                    Email = "sg@mail.com",
                    Note = "note note note",
                    Income = 1234m
                };

                this.onlineContext.Add(onlineSpecificUser);
                this.onlineContext.SaveChanges();

                //lock the Users table in the online storage to simulate that the server is not available
                this.LockDbTable("Users");

                User userFromBackup = null;
                try
                {
                    //attempt a retrival operation
                    //as the server is not available (Users locked) the SmartContext will switch to the backup storage
                    userFromBackup = sCtx.GetSingle<User>(usr => usr.UserId == onlineSpecificUser.UserId);
                }
                catch (Exception)
                {
                    //now the SmartCotnext has already switched to use the backup storage
                }

                //release the lock so that the server would available
                this.ReleaseLock();
                Thread.Sleep(20000);
                //During the thime which the tread sleeps the SmartContext will evalueate that the
                //online storage is available and switch to using it
                
                //As the SmartContext now uses the online storage we can retrieve the online specific user
                User userFromOnline = sCtx.GetSingle<User>(usr => usr.UserId == onlineSpecificUser.UserId);
                Assert.IsNull(userFromBackup);
                Assert.IsNotNull(userFromOnline);
            }
        }

        [TestMethod]
        public void TestAutoSwitchSmartContext_BackupDeletes_SwitchBackToOnlineMode_SyncDeletes_ObjectsShouldBeDeletedFromOnlineStorage()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOnlineCountBefore = this.onlineContext.Users.Count();

                int usrToDeleteId = 1;
                User usrToDelete = sCtx.GetSingle<User>(usr => usr.UserId == usrToDeleteId);
                sCtx.Delete(usrToDelete);

                this.LockDbTable("Users");
                sCtx.SaveChanges();

                this.ReleaseLock();
                Thread.Sleep(20000);

                
                User usrToDeleteFromOnline = this.onlineContext.Users.FirstOrDefault(usr => usr.UserId == usrToDeleteId);
                int usersInOnlineCountAfter = this.onlineContext.Users.Count();
                Assert.AreNotEqual(usersInOnlineCountBefore, usersInOnlineCountAfter);
                Assert.AreEqual(usersInOnlineCountBefore - 1, usersInOnlineCountAfter);
                Assert.IsNull(usrToDeleteFromOnline);
            }
        }

        /// <summary>
        /// locks a specified table
        /// </summary>
        /// <param name="tableName">The name of the table which will be locked</param>
        private void LockDbTable(string tableName)
        {
            this.lockContext.ExecuteNonQuery(@"BEGIN TRAN");
            this.lockContext.ExecuteNonQuery(@"SELECT 1 FROM [" + tableName + "] WITH (XLOCK, TABLOCK)");
        }

        /// <summary>
        /// releases the lock initiated by lockContext
        /// </summary>
        private void ReleaseLock()
        {
            this.lockContext.ExecuteNonQuery("ROLLBACK TRAN");
        }
    }
}
