using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UsersEntitiesModel;
using PermissionsEntitiesModel;
using Telerik.OpenAccess;

namespace ContainsExample
{
    class Program
    {
        static void Main(string[] args)
        {
            List<int> userIDsDropDatabase = new List<int>();
            List<int> userIDsDeleteRecodrds = new List<int>();

            //Retrieve the userIDs
            using (PermissionsModel dbContext = new PermissionsModel())
            {
                //Retrieve the users who can drop the database
                userIDsDropDatabase = dbContext.Permissions
                                             .Where(p => p.DBObjectName == "SofiaCarRentalDb" 
                                                      && p.GrantedPermission == "DROP")
                                             .Select(p => p.UserID).ToList();

                //Retrieve the users who can delete records from any database table
                userIDsDeleteRecodrds = dbContext.Permissions
                                               .Where(p => p.GrantedPermission == "DELETE")
                                               .Select(p => p.UserID).ToList();
            }

            using (UsersModel dbContext = new UsersModel())
            {
                dbContext.Log = Console.Out;
                Console.WriteLine("---------------------Contains with IN clause---------------------");
                List<User> usersDropDatabase = dbContext.Users.Where(u => userIDsDropDatabase.Contains(u.UserID)).ToList();

                Console.WriteLine("\n---------------------Contains with temporary table---------------------");
                List<User> userDeleteRecords = dbContext.Users.Where(u => userIDsDeleteRecodrds.Contains(u.UserID)).ToList();

                Console.WriteLine("\n---------------------ContainsExtended with temporary table---------------------");
                userDeleteRecords = dbContext.Users.Where(u => userIDsDeleteRecodrds.ContainsExtended(u.UserID)).ToList();

                Console.WriteLine("\n---------------------Users that can drop the database---------------------");
                Console.WriteLine("\nName\t\t\t\t\tLast Active On\n");
                foreach (User user in usersDropDatabase)
                {
                    Console.WriteLine("{0}\t\t\t\t{1}", user.UserName, user.LastActiveDate);
                }
                Console.ReadLine();
            }
        }
    }
}
