using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class RefundModel
    {
        public string refund_id { get; set; }
        public string refund_note { get; set; }
        public double refund_amount { get; set; }
        public string order_id { get; set; }
        public string refund_status { get; set; }
    }
}
