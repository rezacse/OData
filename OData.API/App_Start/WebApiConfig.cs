using Microsoft.OData.Edm;
using OData.Model.EntityModels;
using System.Web.Http;
using System.Web.OData.Batch;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;

namespace OData.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //// Web API configuration and services

            //// Web API routes
            //config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);

            config.Count().Filter().OrderBy().Expand().Select().MaxTop(null);
            config.MapODataServiceRoute("ODataRoute", "odata", GetEdmModel(),
                new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));

            config.EnsureInitialized();
        }

        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "OData",
                ContainerName = "ODataContainer"
            };

            builder.EntitySet<Person>("People");
            //builder.EntitySet<VinylRecord>("VinylRecords");
            builder.EntitySet<RecordStore>("RecordStores");

            var isHghRatedFunction = builder.EntityType<RecordStore>().Function("IsHighRated");
            isHghRatedFunction.Returns<bool>();
            isHghRatedFunction.Parameter<int>("minimumRating");
            isHghRatedFunction.Namespace = "OData.Functions";

            var areRatedByFunction = builder.EntityType<RecordStore>().Collection.Function("AreRatedBy");
            areRatedByFunction.ReturnsCollectionFromEntitySet<RecordStore>("RecordStores");
            areRatedByFunction.CollectionParameter<int>("personIds");
            areRatedByFunction.Namespace = "OData.Functions";

            var getHighRatedStoresFunction = builder.Function("GetHighRatedRecordStores");
            getHighRatedStoresFunction.Parameter<int>("minimumRating");
            getHighRatedStoresFunction.ReturnsCollectionFromEntitySet<RecordStore>("RecordStores");
            getHighRatedStoresFunction.Namespace = "OData.Functions";

            var rateAction = builder.EntityType<RecordStore>().Action("Rate");
            rateAction.Returns<bool>();
            rateAction.Parameter<int>("rating");
            rateAction.Parameter<int>("personId");
            rateAction.Namespace = "OData.Actions";

            var removeRatingAction = builder.EntityType<RecordStore>().Collection
                .Action("RemoveRatings");
            removeRatingAction.Returns<bool>();
            removeRatingAction.Parameter<int>("personId");
            removeRatingAction.Namespace = "OData.Actions";

            var removeRecordStoreRatingsAction = builder.Action("RemoveRecordStoreRatings");
            removeRecordStoreRatingsAction.Parameter<int>("personId");
            removeRecordStoreRatingsAction.Namespace = "OData.Actions";

            builder.Singleton<Person>("Tim");

            return builder.GetEdmModel();
        }
    }
}
