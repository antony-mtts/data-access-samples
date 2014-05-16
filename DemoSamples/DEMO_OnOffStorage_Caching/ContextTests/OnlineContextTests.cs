using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Model;

namespace ContextTests
{
    [TestClass]
    public class OnlineContextTests : BaseTestClass
    {
        [ClassInitialize]
        public static void OnlineContextTestsInit(TestContext testCtx)
        {
            contextFactory.SetContextMode(ContextMode.OfflineMode);
        }

        [TestMethod]
        public void TestOnlineInsert_ShouldBeInserted()
        {
            using (FluentModel context = BaseTestClass.contextFactory.GetContext())
            {
                int initialUsersCount = context.Users.Count();

                User newUsr = new User()
                {
                    Name = "Jefry Vanderberg",
                    Note = "note note note",
                    Email = "Jefry@email.com",
                    Income = 1234.123m
                };
                context.Add(newUsr);
                context.SaveChanges();

                int newUsersCount = context.Users.Count();
                User retrievedNewUser = context.Users.FirstOrDefault(usr => usr.Name == newUsr.Name);
                Assert.AreEqual(initialUsersCount + 1, newUsersCount);
                Assert.IsNotNull(retrievedNewUser);
            }
        }

        [TestMethod]
        public void TestOnlineUpdate_ShouldBeUpdated()
        {
            using (FluentModel context = BaseTestClass.contextFactory.GetContext())
            {
                User usrToUpdate = context.Users.FirstOrDefault();
                string newName = "Newman Norton";
                usrToUpdate.Name = newName;
                context.SaveChanges();

                User updatedUsrFromDb = context.Users.FirstOrDefault(usr => usr.UserId == usrToUpdate.UserId);
                Assert.IsNotNull(updatedUsrFromDb);
                Assert.AreEqual(newName, updatedUsrFromDb.Name);
            }
        }

        [TestMethod]
        public void TestOnlineDelete_ShouldBeDeleted()
        {
            using (FluentModel context = BaseTestClass.contextFactory.GetContext())
            {
                User usrToDelete = context.Users.FirstOrDefault();

                context.Delete(usrToDelete);
                context.SaveChanges();

                User deletedUserFromDb = context.Users.FirstOrDefault(usr => usr.UserId == usrToDelete.UserId);
                Assert.IsNull(deletedUserFromDb);
            }
        }
    }
}
