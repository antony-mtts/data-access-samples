using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.OpenAccess;

namespace ContextTests
{
    public class BaseTestClass
    {
        //protected static FluentModel context;
        protected static ContextFactory contextFactory = ContextFactory.FactoryInstance;

        [TestInitialize]
        public void BaseTestInit()
        {
            contextFactory.SetContextMode(ContextMode.OnlineMode);
            using (FluentModel context = contextFactory.GetContext())
            {
                EnsureDatabase(context);
                ClearDatabase(context);
                SeedDatabase(context);
            }
        }

        protected static void EnsureDatabase(FluentModel context)
        {
            context.CreateUpdateDatabase();
        }

        protected static void ClearDatabase(FluentModel context)
        {
            context.Users.DeleteAll();
            context.Groups.DeleteAll();
        }

        protected static void SeedDatabase(FluentModel context)
        {
            User usr1 = new User()
            {
                UserId = 1,
                Name = "Gary Adkins",
                Note = "note note note",
                Email = "Gary@email.com",
                Income = 10000.324m
            };

            User usr2 = new User()
            {
                UserId = 2,
                Name = "Tim Gordon",
                Note = "note note note",
                Email = "Tim@email.com",
                Income = 10000.324m
            };

            User usr3 = new User()
            {
                UserId = 3,
                Name = "Jack Jameson",
                Note = "note note note",
                Email = "Jack@email.com",
                Income = 10000.324m
            };

            User usr4 = new User()
            {
                UserId = 4,
                Name = "Bill Tompson",
                Note = "note note note",
                Email = "Bill@email.com",
                Income = 10000.324m
            };

            Group administratorsGroup = new Group()
            {
                GroupId = 1,
                Name = "Administrators",
                Description = "admin group description",
            };
            administratorsGroup.UsersInGroup.Add(usr1);
            administratorsGroup.UsersInGroup.Add(usr2);

            Group coreUsersGroup = new Group()
            {
                GroupId = 2,
                Name = "Core users",
                Description = "core users description"
            };
            coreUsersGroup.UsersInGroup.Add(usr3);
            coreUsersGroup.UsersInGroup.Add(usr4);

            context.Add(administratorsGroup);
            context.Add(coreUsersGroup);
            context.SaveChanges();
        }
    }
}
