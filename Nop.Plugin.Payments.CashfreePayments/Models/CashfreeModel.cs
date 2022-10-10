using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class CashfreeModel
    {   
        public string order_id { get; set; }
        public double order_amount { get; set; }
        public string order_currency { get; set; }
        public string order_note { get; set; }
        public CustomerDetails customer_details { get; set; }
        public OrderMeta order_meta { get; set; }
    }
}
