using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.CashfreePayments.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //notify_url
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.CashfreePayments.NotifyUrl", "Plugins/PaymentCashfree/NotifyUrl",
                 new { controller = "PaymentCashfree", action = "NotifyUrl" });
            //return_url
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.CashfreePayments.HandleResponse", "Plugins/PaymentCashfree/HandleResponse",
                 new { controller = "PaymentCashfree", action = "HandleResponse" });

        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}