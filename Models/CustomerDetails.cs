using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public  class CustomerDetails
    {
        public string customer_id { get; set; }
        public string customer_name { get; set; }   
        public string customer_email { get; set; }
        public string customer_phone { get; set; }

    }
}
