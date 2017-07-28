using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;

namespace OData.API.Helpers
{
    public static class ODateHelpers
    {

        /// <summary>
        /// OData Helper methods - slightly adjusted from OData helpers provided by Microsoft
        /// </summary>
        public static bool HasProperty(this object instance, string propertyName)
        {
            var propertyInfo = instance.GetType().GetProperty(propertyName);
            return propertyInfo != null;
        }

        public static object GetValue(this object instance, string propertyName)
        {

            var propertyInfo = instance.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new HttpException("Can't find property with name " + propertyName);

            return propertyInfo.GetValue(instance, new object[] { });
        }

        public static IHttpActionResult CreateOkHttpActionResult(this ODataController controller, object propertyValue)
        {
            var okMethod = default(MethodInfo);

            // find the ok method on the current controller
            var methods = controller.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method.Name != "Ok" || method.GetParameters().Length != 1)
                    continue;

                okMethod = method;
                break;
            }

            // invoke the method, passing in the propertyValue
            okMethod = okMethod.MakeGenericMethod(propertyValue.GetType());
            var returnValue = okMethod.Invoke(controller, new[] { propertyValue });
            return (IHttpActionResult)returnValue;
        }

        /// <summary>
        /// Helper method to get the odata path for an arbitrary odata uri.
        /// </summary>
        /// <param name="request">The request instance in current context</param>
        /// <param name="uri">OData uri</param>
        /// <returns>The parsed odata path</returns>
        public static ODataPath CreateODataPath(this HttpRequestMessage request, Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var newRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            var route = request.GetRouteData().Route;

            var newRoute = new HttpRoute(
                route.RouteTemplate,
                new HttpRouteValueDictionary(route.Defaults),
                new HttpRouteValueDictionary(route.Constraints),
                new HttpRouteValueDictionary(route.DataTokens),
                route.Handler);

            // get original configuration
            var configurationFromOriginalRequest = request.GetConfiguration();

            // set configuration on the new request object to match the
            // original configuration
            newRequest.SetConfiguration(configurationFromOriginalRequest);

            var path = configurationFromOriginalRequest.VirtualPathRoot;
            var routeData = newRoute.GetRouteData(path, newRequest);
            if (routeData == null)
                throw new InvalidOperationException("This link is not a valid OData link.");

            return newRequest.ODataProperties().Path;
        }

        public static TKey GetKeyValue<TKey>(this HttpRequestMessage request, Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            //get the odata path Ex: ~/entityset/key/$links/navigation
            var odataPath = request.CreateODataPath(uri);

            var keySegment = odataPath.Segments
                .OfType<Microsoft.OData.UriParser.KeySegment>().LastOrDefault();

            if (keySegment == null || !keySegment.Keys.Any())
                throw new InvalidOperationException("This link does not contain a key.");

            // found a key segment.  Return the key value of the last segment
            return (TKey)keySegment.Keys.Last().Value;
        }

    }
}