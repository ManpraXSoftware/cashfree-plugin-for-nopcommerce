using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class OrderMeta
    {
        public string return_url { get; set; }
        public string  notify_url{ get; set; }
        public string payment_methods { get; set; }

    }
}
