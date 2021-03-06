﻿using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore3xEndpointSample.OData
{
    public class MatchAllRoutingConvention : IODataRoutingConvention
    {
        private readonly string ControllerName = "HandleAll";

        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            var odataPath = routeContext.HttpContext.ODataFeature().Path;

            if (!(odataPath.Segments.FirstOrDefault() is EntitySetSegment))
            {
                return Enumerable.Empty<ControllerActionDescriptor>();
            }

            // Get a IActionDescriptorCollectionProvider from the global service provider.
            IActionDescriptorCollectionProvider actionCollectionProvider =
                routeContext.HttpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();

            IEnumerable<ControllerActionDescriptor> actionDescriptors = actionCollectionProvider
                    .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                    .Where(c => c.ControllerName == ControllerName);

            if (odataPath.PathTemplate == "~/entityset/key/navigation")
            {
                if (routeContext.HttpContext.Request.Method.ToUpperInvariant() == "GET")
                {
                    NavigationPropertySegment navigationPathSegment = (NavigationPropertySegment)odataPath.Segments.Last();

                    routeContext.RouteData.Values["navigation"] = navigationPathSegment.NavigationProperty.Name;

                    KeySegment keyValueSegment = (KeySegment)odataPath.Segments[1];
                    routeContext.RouteData.Values[ODataRouteConstants.Key] = keyValueSegment.Keys.First().Value;

                    return actionDescriptors.Where(c => c.ActionName == "GetNavigation").ToList();
                }
            }

            SelectControllerResult controllerResult = new SelectControllerResult(ControllerName, null);
            IList<IODataRoutingConvention> routingConventions = ODataRoutingConventions.CreateDefault();
            foreach (NavigationSourceRoutingConvention nsRouting in routingConventions.OfType<NavigationSourceRoutingConvention>())
            {
                string actionName = nsRouting.SelectAction(routeContext, controllerResult, actionDescriptors);
                if (!string.IsNullOrEmpty(actionName))
                {
                    return actionDescriptors.Where(
                        c => string.Equals(c.ActionName, actionName, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return null;
        }
    }
}
