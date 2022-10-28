using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public partial class UPIAuthorizedDetails
    {   
       public string approve_by { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
    }
}
