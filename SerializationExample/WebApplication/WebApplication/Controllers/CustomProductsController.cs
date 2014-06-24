using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using WebApplicationModel;

namespace WebApplication
{
    public class CustomProductsController : ApiController
    {
        protected EntitiesModel dbContext;

        public CustomProductsController()
        {
            this.dbContext = new EntitiesModel();
        }

        private IQueryable<ProductCustomized> Get()
        {
            System.Linq.IQueryable<ProductCustomized> entities =
                from p in this.dbContext.Products
                select new ProductCustomized()
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitPrice = p.UnitPrice
                };

            return entities;
        }

        public virtual PageResult<ProductCustomized> Get(ODataQueryOptions<ProductCustomized> options)
        {
            System.Linq.IQueryable<ProductCustomized> entities = this.Get();

            var result = options.ApplyTo(entities);

            return new PageResult<ProductCustomized>(result as IEnumerable<ProductCustomized>, Request.GetNextPageLink(), Request.GetInlineCount());
        }

        public virtual WebApplicationModel.ProductCustomized Get(Int32 id)
        {
            WebApplicationModel.ProductCustomized entity = this.GetBy(w => w.ProductID == id);

            if (entity == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return entity;
        }

        private ProductCustomized GetBy(System.Linq.Expressions.Expression<System.Func<ProductCustomized, bool>> filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            ProductCustomized entity = this.Get().SingleOrDefault(filter);

            if (entity == null)
                return default(ProductCustomized);

            return entity;
        }

        protected HttpResponseMessage CreateResponse(HttpStatusCode httpStatusCode, WebApplicationModel.ProductCustomized entityToEmbed)
        {
            HttpResponseMessage response = Request.CreateResponse<WebApplicationModel.ProductCustomized>(httpStatusCode, entityToEmbed);

            string uri = Url.Link("DefaultApi", new { id = entityToEmbed.ProductID });
            response.Headers.Location = new Uri(uri);

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (dbContext != null)
            {
                dbContext.Dispose();
            }
        }
    }
}