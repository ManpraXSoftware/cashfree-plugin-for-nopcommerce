using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class SubscriptionModel
    {

        /// <summary>
        /// ID of a valid plan created earlier.
        /// </summary>
        public string planId { get; set; }


        /// <summary>
        /// A unique ID generated for subscription. It can include alphanumerical characters, underscore, dot, hyphens, and spaces.
        /// </summary>
        public string subscriptionId { get; set; }


        /// <summary>
        /// Name of the customer.
        /// </summary>
        public string customerName { get; set; }


        /// <summary>
        /// Email of the customer.
        /// </summary>
        public string customerEmail { get; set; }


        /// <summary>
        /// Contact number of the customer.
        /// </summary>
        public string customerPhone { get; set; }


        /// <summary>
        /// the date at which the first payment takes place. Applicable for Periodic Subscriptions only.(date pattern yyyy-MM-dd)
        /// </summary>
       // public DateTime firstChargeDate { get; set; }


        /// <summary>
        /// the amount that will be charged to authenticate the payment and is applicable for UPI and Cards only. The default amount is Re. 1/-. Note: This amount is not refunded to the customer.
        /// </summary>
        public double authAmount { get; set; }


        /// <summary>
        /// (Date,pattern :yyyy-MM-dd HH:mm:ss) This is the date the subscription stands invalid, and no longer active. Any charge raised on this date, will not be deducted. The default value is 2 years from date of subscription creation.
        /// </summary>
       // public DateTime expiresOn { get; set; }


        /// <summary>
        /// A valid url to which customer will be redirected to after the subscription is done. Refer “Payment Response” section.
        /// </summary>
        public string returnUrl { get; set; }


        /// <summary>
        /// A brief note about the subscription
        /// </summary>
        public string subscriptionNote { get; set; }


        /// <summary>
        /// Possible values: EMAIL, SMS. The checkout link is sent through the given notification channels.
        /// </summary>
        public string[] notificationChannels { get; set; }

    }
}
