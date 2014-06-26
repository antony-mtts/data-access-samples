using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using Telerik.OpenAccess.FetchOptimization;
using WebApplicationModel;

namespace WebApplication
{
    public class CustomProductsController : ApiController
    {
        protected EntitiesModel dbContext;
        protected FetchStrategy fetchStrategy;

        public CustomProductsController()
        {
            this.dbContext = new EntitiesModel();
            this.fetchStrategy = new FetchStrategy();
        }

        ///<summary>
        ///Retrieves all the objects of a given persistent entity.
        ///</summary>
        ///<param name="fetchStrategy">
        ///Specifies which navigation properties of the objects should be populated
        ///when the objects are retrieved from the database.
        ///</param>
        ///<returns>
        ///The query that will be executed when the result is enumerated.
        ///</returns>
        private IQueryable<T> Get<T>(FetchStrategy fetchStrategy)
        {
            if (fetchStrategy != null)
            {
                this.dbContext.FetchStrategy = fetchStrategy;                
            }

            IQueryable<T> entities = this.dbContext.GetAll<T>();
                
            return entities;
        }

        ///<summary>
        ///Retrieves a subset of the Product objects.
        ///</summary>
        ///<param name="filter">
        ///Specifies the filter that will be applied on the Product objects.
        ///</param>
        ///<returns>
        ///The query that will be executed when the result is enumerated.
        ///</returns>
        private IQueryable<Product> GetBy(Expression<Func<Product, bool>> filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            fetchStrategy.LoadWith<Product>(p => p.Category);

            IQueryable<Product> entity = this.Get<Product>(fetchStrategy).Where(filter);

            return entity;
        }

        ///<summary>
        ///Retrieves a paged result for all ProductCustomized objects.
        ///</summary>
        ///<returns>
        ///The query that will be executed when the result is enumerated.
        ///</returns>
        public virtual PageResult<ProductCustomized> Get(ODataQueryOptions<Product> options)
        {
           IQueryable<Product> entities = this.Get<Product>(new FetchStrategy());

           entities = options.ApplyTo(entities) as IQueryable<Product>;

            var result = from e in entities
                         select new ProductCustomized()
                         {
                             ProductID = e.ProductID,
                             ProductName = e.ProductName,
                             CategoryName = e.Category.CategoryName,
                             QuantityPerUnit = e.QuantityPerUnit,
                             UnitPrice = e.UnitPrice
                         };

            return new PageResult<ProductCustomized>(result.AsEnumerable(), Request.GetNextPageLink(), Request.GetInlineCount());
        }

        ///<summary>
        ///Retrieves ProductCustomized objects by Id.
        ///</summary>
        ///<param name="id">
        ///The Id of the Product object that should be retrieved
        ///</param>
        ///<returns>
        ///The ProductCustomized object with the specified Id.
        ///</returns>
        public virtual ProductCustomized Get(Int32 id)
        {
            IQueryable<Product> entity = this.GetBy(w => w.ProductID == id);

            if (entity == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            Product product = entity.FirstOrDefault();

            ProductCustomized result = new ProductCustomized()
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                CategoryName = product.Category.CategoryName,
                QuantityPerUnit = product.QuantityPerUnit,
                UnitPrice = product.UnitPrice
            };

            return result;
        }

        ///<summary>
        ///Retrieves ProductCustomized objects by name.
        ///</summary>
        ///<param name="name">
        ///The name of the products that should be retrieved.
        ///</param>
        ///<returns>
        ///The ProductCustomized object with name that matches exactly the specified name.
        ///</returns>
        public virtual IQueryable<ProductCustomized> Get(String name)
        {
            IQueryable<Product> entities = this.GetBy(w => w.ProductName == name);

            if (entities == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var result = from e in entities
                        select new ProductCustomized()
                        {
                            ProductID = e.ProductID,
                            ProductName = e.ProductName,
                            CategoryName = e.Category.CategoryName,
                            QuantityPerUnit = e.QuantityPerUnit,
                            UnitPrice = e.UnitPrice
                        };

            return result;
        }

        ///<summary>
        ///Disposes the instance of the context.
        ///</summary>
        protected override void Dispose(bool disposing)
        {
            if (dbContext != null)
            {
                dbContext.Dispose();
            }
        }
    }
}