using Nop.Core.Configuration;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.CashfreePayments
{
    /// <summary>
    /// Represents settings of the cashfree payment plugin
    public class CashfreePaymentSettings : ISettings
    {
        //In the Edit Settings screen, enter the Title, App ID, Secret Key, Payment Mode (Test/Prod) and the Success Message
        public string Title { get; set; }
        public string AppID { get; set; }
        public string SecretKey { get; set; }
        public string Description { get; set; }
        //Active envt similar to Payment Mode (Test Mode/Live Mode)
        //public string? ActiveEnvironment { get; set; }
        public ActiveEnvironment ActiveEnvironment { get; set; }
        public string PaymentMethods { get; set; }
        public decimal AdditionalFee { get; set; }

        public bool AdditionalFeePercentage { get; set; }
        //ApiVersion
         public string ApiVersion { get; set; }
        /// <summary>
        /// Gets or sets the payment type
        /// </summary>
        public PaymentType PaymentType { get; set; }


    }
}
