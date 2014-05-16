using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using System.Linq;
using System.Linq.Expressions;
using Telerik.OpenAccess.FetchOptimization;
using System.Collections.Generic;
using System.Linq.Dynamic;
using Telerik.OpenAccess;

namespace ContextTests
{
    [TestClass]
    public class SmartContextTests : BaseTestClass
    {
        private FluentModel offlineContext;

        [TestInitialize]
        public void TestInit()
        {
            base.BaseTestInit();
            BaseTestClass.contextFactory.SetContextMode(ContextMode.OfflineMode);
            using (FluentModel context = BaseTestClass.contextFactory.GetContext())
            {
                BaseTestClass.EnsureDatabase(context);
                BaseTestClass.ClearDatabase(context);
                //clear the artificial type DeleteOperation
                context.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName).DeleteAll();
            }

            this.offlineContext = BaseTestClass.contextFactory.GetContext();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.offlineContext.DisposeDatabase();
            this.offlineContext.Dispose();
        }

        [TestMethod]
        public void TestSmartContext_RetrieveUser_ShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();

                User usr = sCtx.GetSingle<User>();
                Assert.IsNotNull(usr);

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                User usrFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(u => u.UserId == usr.UserId);

                Assert.IsNotNull(usrFromOfflineStorage);
                Assert.AreEqual(usersInOfflineCountBefore + 1, usersInOfflineCountAfter);
            }
        }

        [TestMethod]
        public void TestSmartContext_RetreveUserMultipleTimes_ShouldBeSavedToOfflineOnlyOnce()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();

                User usr1 = sCtx.GetSingle<User>();
                User sameUsr1 = sCtx.GetSingle<User>();
                User sameUsr2 = sCtx.GetSingle<User>();
                
                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                User usrFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(u => u.UserId == usr1.UserId);

                Assert.IsNotNull(usr1);
                Assert.IsNotNull(usrFromOfflineStorage);
                Assert.AreEqual(usersInOfflineCountBefore + 1, usersInOfflineCountAfter);
            }
        }

        [TestMethod]
        public void TestSmartContext_RetrieveUserWithCondition_ShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();

                User usr = sCtx.GetSingle<User>((u) => u.UserId == 3);

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                User usrFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(u => u.UserId == usr.UserId);

                Assert.IsNotNull(usr);
                Assert.AreEqual(3, usr.UserId);
                Assert.IsNotNull(usrFromOfflineStorage);
                Assert.AreEqual(usersInOfflineCountBefore + 1, usersInOfflineCountAfter);
            }
        }

        [TestMethod]
        public void TestSmartContext_RetrieveUserAndRelatedGroup_BothShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();
                int groupsInOfflineCountBefore = this.offlineContext.Groups.Count();

                FetchStrategy loadWithGroup = new FetchStrategy();
                loadWithGroup.LoadWith<User>(u => u.Group);
                User usr = sCtx.GetSingle<User>(fetchStrategy: loadWithGroup);


                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                int groupsInOfflineCountAfter = this.offlineContext.Groups.Count();
                User userFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(u => u.UserId == usr.UserId);
                Group groupFromOfflineStorage = this.offlineContext.Groups.FirstOrDefault(grp => grp.GroupId == usr.GroupId);

                Assert.IsNotNull(usr);
                Assert.IsNotNull(userFromOfflineStorage);
                Assert.IsNotNull(groupFromOfflineStorage);
                Assert.AreEqual(usersInOfflineCountBefore + 1, usersInOfflineCountAfter);
                Assert.AreEqual(groupsInOfflineCountBefore + 1, usersInOfflineCountAfter);
            }
        }

        [TestMethod]
        public void TestSmartContext_RetrieveMultipleUsers_AllUsersShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();

                IEnumerable<User> users = sCtx.GetMultiple<User>();

                IList<int> userIds = users.Select(usr => usr.UserId).ToList();

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                IEnumerable<User> usersFromOfflineStorage = this.offlineContext.Users.Where(usr => userIds.Contains(usr.UserId)).ToList();

                Assert.AreNotEqual(0, users.Count());
                Assert.AreNotEqual(usersInOfflineCountBefore, usersInOfflineCountAfter);
                Assert.AreEqual(users.Count(), usersFromOfflineStorage.Count());
            }
        }

        [TestMethod]
        public void TestSmartContext_RetrieveMultipleUsersWithCondition_AllUsersShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();

                IEnumerable<User> users = sCtx.GetMultiple<User>((u) => u.UserId > 1);

                IList<int> userIds = new List<int>();
                foreach (User user in users)
                {
                    userIds.Add(user.UserId);
                }

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                IEnumerable<User> usersFromOfflineStorage = this.offlineContext.Users.Where(usr => userIds.Contains(usr.UserId)).ToList();

                Assert.AreNotEqual(0, users.Count());
                Assert.AreNotEqual(usersInOfflineCountBefore, usersInOfflineCountAfter);
                Assert.AreEqual(users.Count(), usersFromOfflineStorage.Count());
            }
        }

        [TestMethod]
        public void TestSmartContext_RetrieveMultipleUsersAndRelatedGroups_AllUsersAndGroupsShouldBeSavedToOffline()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();
                int groupsInOfflineCountBefore = this.offlineContext.Groups.Count();

                FetchStrategy loadWithGroup = new FetchStrategy();
                loadWithGroup.LoadWith<User>(u => u.Group);

                IEnumerable<User> users = sCtx.GetMultiple<User>(fetchStrategy: loadWithGroup);

                IList<int> userIds = users.Select(usr => usr.UserId).ToList();

                IList<int> groupIds = users.Select(usr => usr.GroupId).Distinct().ToList();

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                int groupsInOfflineCountAfter = this.offlineContext.Groups.Count();
                IEnumerable<User> usersFromOfflineStorage = this.offlineContext.Users.Where(usr => userIds.Contains(usr.UserId)).ToList();
                IEnumerable<Group> groupsFromOfflineStorage = this.offlineContext.Groups.Where(grp => groupIds.Contains(grp.GroupId)).ToList();

                Assert.AreNotEqual(0, users.Count());
                Assert.AreNotEqual(usersInOfflineCountBefore, usersInOfflineCountAfter);
                Assert.AreNotEqual(groupsInOfflineCountBefore, groupsInOfflineCountAfter);
                Assert.AreEqual(users.Count(), usersFromOfflineStorage.Count());
                Assert.AreEqual(groupIds.Count, groupsFromOfflineStorage.Count());
            }
        }

        [TestMethod]
        public void TestSmartContext_UpdateUser_UpdatesShouldBeBackedUp()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                User user = sCtx.GetSingle<User>();

                string newUserName = "New Awesome Name";
                user.Name = newUserName;
                sCtx.SaveChanges();

                User userFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(usr => usr.UserId == user.UserId);

                Assert.IsNotNull(userFromOfflineStorage);
                Assert.AreEqual(newUserName, userFromOfflineStorage.Name);
            }
        }

        [TestMethod]
        public void TestSmartContext_InsertEntity_EntityShouldBeBackedUp()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();
 
                User newUser = new User()
                {
                    UserId = 1337,
                    Name = "Mark",
                    Email= "Mark@mail.com",
                    Note = "note note note",
                    Income = 15543m
                };

                sCtx.Add(newUser);
                sCtx.SaveChanges();

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                User userFromOfflineStorage = this.offlineContext.Users.FirstOrDefault(usr => usr.UserId == newUser.UserId);

                Assert.IsNotNull(userFromOfflineStorage);
                Assert.AreEqual(usersInOfflineCountBefore + 1, usersInOfflineCountAfter);
            }
        }

        [TestMethod]
        public void TestSmartContext_InsertUsersWithNewRelatedGroup_AllShouldBeBackedUp()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();
                int groupsInOfflineCountBefore = this.offlineContext.Groups.Count();

                Group newGroup = new Group()
                {
                    GroupId = 1227,
                    Name = "New Group",
                    Description = "Group of newness"
                };

                User newUser1 = new User()
                {
                    UserId = 1337,
                    Name = "Mark",
                    Email = "Mark@mail.com",
                    Note = "note note note",
                    Income = 15543m
                };
                newUser1.Group = newGroup;

                User newUser2 = new User()
                {
                    UserId = 1338,
                    Name = "Harry",
                    Email = "Harry@mail.com",
                    Note = "note note note",
                    Income = 12543m
                };
                newUser2.Group = newGroup;

                sCtx.Add(newUser1);
                sCtx.Add(newUser2);
                sCtx.SaveChanges();

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                int groupsInOfflineCountAfter = this.offlineContext.Groups.Count();
                IEnumerable<User> usersFromOfflineStorage = this.offlineContext.Users.Where(usr => usr.UserId == newUser1.UserId || usr.UserId == newUser2.UserId).ToList();
                IEnumerable<Group> groupsFromOfflineStorage = this.offlineContext.Groups.Where(grp => grp.GroupId == newGroup.GroupId).ToList();

                Assert.AreNotEqual(usersInOfflineCountBefore, usersInOfflineCountAfter);
                Assert.AreNotEqual(groupsInOfflineCountBefore, groupsInOfflineCountAfter);
                Assert.AreEqual(2, usersFromOfflineStorage.Count());
                Assert.AreEqual(1, groupsFromOfflineStorage.Count());
            }
        }

        [TestMethod]
        public void TestSmartContext_InsertUsersWithExistingRelatedGroup_AllShouldBeBackedUp()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                int usersInOfflineCountBefore = this.offlineContext.Users.Count();
                int groupsInOfflineCountBefore = this.offlineContext.Groups.Count();

                //The existing related group will actually be backed up the moment
                //it is retrieved from the SmartContext
                Group relatedGroup = sCtx.GetSingle<Group>();

                User newUser1 = new User()
                {
                    UserId = 1337,
                    Name = "Mark",
                    Email = "Mark@mail.com",
                    Note = "note note note",
                    Income = 15543m
                };
                newUser1.Group = relatedGroup;

                User newUser2 = new User()
                {
                    UserId = 1338,
                    Name = "Harry",
                    Email = "Harry@mail.com",
                    Note = "note note note",
                    Income = 12543m
                };
                newUser2.Group = relatedGroup;

                sCtx.Add(newUser1);
                sCtx.Add(newUser2);
                sCtx.SaveChanges();

                int usersInOfflineCountAfter = this.offlineContext.Users.Count();
                int groupsInOfflineCountAfter = this.offlineContext.Groups.Count();
                IEnumerable<User> usersFromOfflineStorage = this.offlineContext.Users.Where(usr => usr.UserId == newUser1.UserId || usr.UserId == newUser2.UserId).ToList();
                IEnumerable<Group> groupsFromOfflineStorage = this.offlineContext.Groups.Where(grp => grp.GroupId == relatedGroup.GroupId).ToList();

                Assert.AreNotEqual(usersInOfflineCountBefore, usersInOfflineCountAfter);
                Assert.AreNotEqual(groupsInOfflineCountBefore, groupsInOfflineCountAfter);
                Assert.AreEqual(2, usersFromOfflineStorage.Count());
                Assert.AreEqual(1, groupsFromOfflineStorage.Count());
            }
        }

        [TestMethod]
        public void TestSmartContext_DeleteEntity_DeleteShouldBeBackedUp()
        {
            using (SmartContext sCtx = new SmartContext())
            {
                User userToDelete = sCtx.GetSingle<User>();

                int userToDeleteId = userToDelete.UserId;

                sCtx.Delete(userToDelete);
                sCtx.SaveChanges();

                User deletedUsserFromOffline = this.offlineContext.Users.FirstOrDefault(usr => usr.UserId == userToDelete.UserId);

                object deleteOperation = this.offlineContext.GetAll(DeleteOperationDefinition.DeleteOperationFullTypeName)
                    .Cast<object>()
                    .FirstOrDefault(o => o.FieldValue<int>(DeleteOperationDefinition.EntityToDeleteId) == userToDeleteId);

                int deletedEntityId = deleteOperation.FieldValue<int>(DeleteOperationDefinition.EntityToDeleteId);
                string deletedEntityType = deleteOperation.FieldValue<string>((DeleteOperationDefinition.EntityToDeleteType));

                Assert.IsNull(deletedUsserFromOffline);
                Assert.AreEqual(userToDeleteId, deletedEntityId);
                Assert.AreEqual("User", deletedEntityType);
            }
        }
    }
}
