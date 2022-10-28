using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Payments.CashfreePayments.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get;set; }

        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.Title")]
        public string Title { get; set; }
        public bool Title_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.AppID")]
        public string AppID { get; set; }
        public bool AppID_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.SecretKey")]
        public string SecretKey { get; set; }
        public bool SecretKey_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.Description")]
        public string Description { get; set; }
        public bool Description_OverrideForStore { get; set; }
        public int ActiveEnvironmentId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.ActiveEnvironment")]
        public SelectList ActiveEnvironmentValues { get; set; }
        public bool ActiveEnvironmentId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.ApiVersion")]
        public string ApiVersion { get; set; }
        public bool ApiVersion_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.PaymentMethods")]
        public string PaymentMethods { get; set; }
        public bool PaymentMethods_OverrideForStore { get; set; }
        public int PaymentTypeId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.PaymentType")]
        public SelectList PaymentTypeValues { get; set; }
        public bool PaymentTypeId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CashfreePayments.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }


    }
}