using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public class ResponseModel
    {
        public string payment_link { get; set; }
        public string order_token { get; set; }
        public string url { get; set; }
        public Data data { get; set; }
        public string cf_payment_id { get; set; }
        /// <summary>
        /// Subscription Response Data
        /// </summary>
        public string planId { get; set; }
        public string authLink { get; set; }
        public string subStatus { get; set; }
        public string subReferenceId { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }
}
