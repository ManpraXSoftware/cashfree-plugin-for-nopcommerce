using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CashfreePayments.Components
{
    [ViewComponent(Name = "PaymentCashfree")]
    public class PaymentCashfreeViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.CashfreePayments/Views/PaymentInfo.cshtml");
        }
    }
}
