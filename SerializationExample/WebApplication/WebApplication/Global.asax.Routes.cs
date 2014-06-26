using System.Web.Http;
using System.Web.Routing;

namespace WebApplication
{
    public partial class Global
    {
        public void ConfigureWebApi()
        {
            RouteTable.Routes.MapHttpRoute(name: "DefaultNameApi",
                                           routeTemplate: "api/{controller}/{name}",
                                           defaults: new { name = RouteParameter.Optional });

            RouteTable.Routes.MapHttpRoute(name: "DefaultApi",
                                           routeTemplate: "api/{controller}/{id}",
                                           defaults: new { id = RouteParameter.Optional });

            ApplyCustomConfiguration();
        }

        partial void ApplyCustomConfiguration();
    }
}
