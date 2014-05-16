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
