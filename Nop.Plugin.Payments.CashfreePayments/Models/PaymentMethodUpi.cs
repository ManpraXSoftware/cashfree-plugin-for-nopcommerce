using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class PaymentMethodUpi
    {   
     //  public CardPaymentMethod card { get; set; }
        public UpiPaymentMethod upi { get; set; }
    }
    public class UpiPaymentMethod
    {
        public string channel { get; set; }
        public string upi_id { get; set; }
        public string upi_expiry_minutes { get; set; }
        public bool authorize_only { get; set; }
        public UPIAuthorizedDetails authorization { get; set; }
    }
}
