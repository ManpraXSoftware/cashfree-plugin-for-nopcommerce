using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public class AuthorizationModel
    {
        public string action { get; set; }
        public double amount { get; set; }

    }
}
