using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
     public partial class PayOrderCardModel
    {   
        public string order_token { get; set; }
        public PaymentMethodCard payment_method { get; set; }
        //public bool save_instrument { get; set; }
    }
    public partial class PaymentMethodCard
    {
        public CardPaymentMethod card { get; set; }

    }
    public class CardPaymentMethod
    {
        public string channel { get; set; }
        public string card_number { get; set; }
        public string card_holder_name { get; set; }
        public string card_expiry_mm { get; set; }
        public string card_expiry_yy { get; set; }
        public string card_cvv { get; set; }
    }
}
