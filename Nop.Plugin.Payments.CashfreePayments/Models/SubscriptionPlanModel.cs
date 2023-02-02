using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class SubscriptionPlanModel
    {
        public string planId { get; set; }
        public string planName { get; set; }
        public string type { get; set; }
        public int maxCycles { get; set; }
        public double amount { get; set; }
        public string intervalType { get; set; }
        public int intervals { get; set; }
        public string description { get; set; }

    }
}
