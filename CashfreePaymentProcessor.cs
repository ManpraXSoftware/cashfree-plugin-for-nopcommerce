using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Orders;
using Nop.Services.Payments;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Configuration;
using Nop.Core;
using Nop.Services.Orders;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;
using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Nop.Plugin.Payments.CashfreePayments.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Payments.CashfreePayments
{
    public class CashfreePaymentProcessor : BasePlugin, IPaymentMethod
    {

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IAddressService _addressService;
        private readonly ICurrencyService _currencyService;
        private readonly CashfreePaymentSettings _cashfreePaymentSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;

        public CashfreePaymentProcessor(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService,
            IAddressService addressService,
            ICurrencyService currencyService,
            CashfreePaymentSettings cashfreePaymentSettings,
            CurrencySettings currencySettings,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            IWorkContext workContext,
            IStoreContext storeContext,
            IOrderService orderService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _orderTotalCalculationService = orderTotalCalculationService;
            _addressService = addressService;
            _currencyService = currencyService;
            _cashfreePaymentSettings = cashfreePaymentSettings;
            _currencySettings = currencySettings;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _workContext = workContext;
            _storeContext = storeContext;
            _orderService = orderService;
        }

        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new CashfreePaymentSettings
            {
                
            });
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.CashfreePayments.Fields.Title"] = "Title",
                ["Plugins.Payments.CashfreePayments.Fields.Title.Hint"] = "Enter Title",
                ["Plugins.Payments.CashfreePayments.Fields.AppID"] = "App ID",
                ["Plugins.Payments.CashfreePayments.Fields.AppID.Hint"] = "Specify AppID for your Cashfree Account",
                ["Plugins.Payments.CashfreePayments.Fields.SecretKey"] = "Secret Key",
                ["Plugins.Payments.CashfreePayments.Fields.SecretKey.Hint"] = "Specify Secret Key for your Cashfree Account",
                ["Plugins.Payments.CashfreePayments.Fields.Description"] = "Description",
                ["Plugins.Payments.CashfreePayments.Fields.Description.Hint"] = "Enter Description",
                ["Plugins.Payments.CashfreePayments.Fields.ActiveEnvironment"] = "Active Environment",
                ["Plugins.Payments.CashfreePayments.Fields.ActiveEnvironment.Hint"] = "Specify Active Environment",
                ["Plugins.Payments.CashfreePayments.Fields.PaymentMethods"] = "Payment Methods",
                ["Plugins.Payments.CashfreePayments.Fields.PaymentMethods.Hint"] = "Specify payment methods ",
                ["Plugins.Payments.CashfreePayments.Fields.ApiVersion"] = "Api Version",
                ["Plugins.Payments.CashfreePayments.Fields.ApiVersion.Hint"] = "Specify Api Version",
                ["Plugins.Payments.CashfreePayments.Fields.RedirectionTip"] = "You will be redirected to Cashfree site to complete the order.",
                ["Plugins.Payments.CashfreePayments.Fields.Instructions"] = @"
                    <p>
                        <b>If you're using this gateway ensure that your primary store currency is supported by Cashfree.</b>
	                    <br />
                        <br />Follow these steps to configure your account for Payment:<br />
                        <br />1. Log in to your Cashfree account (click <a href=""https://www.cashfree.com/"" target=""_blank"">here</a> to create your account).
                        <br />2. Activate your Account (follow this <a href=""https://docs.cashfree.com/docs/activate-account"" target=""_blank"">document</a> to activate your account).
                        <br />3. Click on <b>developers </b>
                        <br />4. Click on <b>API Keys</b> under Payment Gateway and get your <b>App ID</b> and <b>Secret key</b>.
                        <br />5. Enter <b>App ID</b> and <b>Secret Key</b> in the field below on the plugin configuration page.
                        <br />6. Click <b>Save</b> button on this page.
                        <br />
                    </p>",
            });
            await base.InstallAsync();
        }
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<CashfreePaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.CashfreePayments");

            await base.UninstallAsync();
        }
         public bool SupportCapture => false;
        public bool SupportPartiallyRefund => true;
        public bool SupportRefund => true;
        public bool SupportVoid => false;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        //The capture workflow helps you to capture the payment and move it from customers bank account to your bank account.
        public async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            HttpRequestMessage request;
            HttpResponseMessage response;

            var order_id = capturePaymentRequest.Order.Id.ToString();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://sandbox.cashfree.com/pg/orders/"+order_id+"/authorization" :
                "https://api.cashfree.com/pg/orders/"+ order_id +"/authorization";

            string responsebody;
            var client = new HttpClient();
            request = new HttpRequestMessage(HttpMethod.Post, url);
            var stringdata = JsonConvert.SerializeObject(new AuthorizationModel()
            {
                action = "CAPTURE",
                amount = 2.0
            });
            var stringcontent = new StringContent(stringdata, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            List<NameValueHeaderValue> listheaders = new List<NameValueHeaderValue>();
            listheaders.Add(new NameValueHeaderValue("x-api-version", _cashfreePaymentSettings.ApiVersion));
            listheaders.Add(new NameValueHeaderValue("x-client-id", _cashfreePaymentSettings.AppID));
            listheaders.Add(new NameValueHeaderValue("x-client-secret", _cashfreePaymentSettings.SecretKey));
            foreach (var header in listheaders)
            {
                request.Headers.Add(header.Name, header.Value);
            }
            response = await client.SendAsync(request);
            responsebody = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);

            if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest" || response.ReasonPhrase == "400")
            {
                throw new Exception(await _localizationService.GetResourceAsync(responsebody));

            }
            if(result.payment_status == "SUCCESS")
            {

            }

            return new CapturePaymentResult
            {
                CaptureTransactionId = result.authId,
                CaptureTransactionResult = result.status,
                NewPaymentStatus = PaymentStatus.Paid
            };
        }

        public async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            HttpRequestMessage request;
            HttpResponseMessage response;

            var order_id = voidPaymentRequest.Order.Id.ToString();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://sandbox.cashfree.com/pg/orders/"+order_id+"/authorization" :
                "https://api.cashfree.com/pg/orders/"+order_id+"/authorization";

            string responsebody;
            var client = new HttpClient();
            request = new HttpRequestMessage(HttpMethod.Post, url);
            var stringdata = JsonConvert.SerializeObject(new AuthorizationModel()
            {
                action = "VOID"
            });
            var stringcontent = new StringContent(stringdata, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            List<NameValueHeaderValue> listheaders = new List<NameValueHeaderValue>();
            listheaders.Add(new NameValueHeaderValue("x-api-version", _cashfreePaymentSettings.ApiVersion));
            listheaders.Add(new NameValueHeaderValue("x-client-id", _cashfreePaymentSettings.AppID));
            listheaders.Add(new NameValueHeaderValue("x-client-secret", _cashfreePaymentSettings.SecretKey));
            foreach (var header in listheaders)
            {
                request.Headers.Add(header.Name, header.Value);
            }
            response = await client.SendAsync(request);
            responsebody = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);

            if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest" || response.ReasonPhrase == "400")
            {
                throw new Exception(await _localizationService.GetResourceAsync(responsebody));

            }
            return new VoidPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Voided
            };
        }

        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                 _cashfreePaymentSettings.AdditionalFee, _cashfreePaymentSettings.AdditionalFeePercentage);

        }

        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.CashfreePayments.PaymentMethodDescription");

        }

        public string GetPublicViewComponentName()
        {
            return "PaymentCashfree";
        }

        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }       

       
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());

        }
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //get current store
            var store = await _storeContext.GetCurrentStoreAsync();

            //get current customer
            var customer = await _workContext.GetCurrentCustomerAsync();

            //get current order details
            var order = (await _orderService.SearchOrdersAsync(store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();

            //get address of customer
            var address = await _addressService.GetAddressByIdAsync(Convert.ToInt32(customer.BillingAddressId));

            //get store location
            var storeLocation = _webHelper.GetStoreLocation();

            HttpRequestMessage request;
            HttpResponseMessage response;
            string responsebody;
            var client = new HttpClient();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://sandbox.cashfree.com/pg/orders" :
                "https://api.cashfree.com/pg/orders";      
            
            request = new HttpRequestMessage(HttpMethod.Post, url);
            var stringdata = JsonConvert.SerializeObject(new CashfreeModel()
            {
                order_id = order.Id.ToString(),
                order_amount = Convert.ToDouble(order.OrderTotal),
                order_currency = order.CustomerCurrencyCode,
                order_note = "order:"+ order.Id,
                customer_details = new CustomerDetails()
                {
                    customer_id = customer.Id.ToString(),
                    customer_name = address.FirstName,
                    customer_email = customer.Email,
                    customer_phone = address.PhoneNumber
                },
                order_meta = new OrderMeta()
                { 
                    return_url = $"{storeLocation}Plugins/PaymentCashfree/HandleResponse?order_id={{order_id}}&order_token={{order_token}}",                 
                    notify_url = $"{storeLocation}Plugins/PaymentCashfree/NotifyUrl",
                    payment_methods = _cashfreePaymentSettings.PaymentMethods
                }
            });

            var stringcontent = new StringContent(stringdata, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            List<NameValueHeaderValue> listheaders = new List<NameValueHeaderValue>();
            listheaders.Add(new NameValueHeaderValue("x-api-version", _cashfreePaymentSettings.ApiVersion));
            listheaders.Add(new NameValueHeaderValue("x-client-id", _cashfreePaymentSettings.AppID));
            listheaders.Add(new NameValueHeaderValue("x-client-secret", _cashfreePaymentSettings.SecretKey));
            foreach (var header in listheaders)
            {
                request.Headers.Add(header.Name, header.Value);
            }

            response = await client.SendAsync(request);
            responsebody = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);

            if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest")
            {
                throw new Exception(await _localizationService.GetResourceAsync(responsebody));

            }            
            var payment_link = result.payment_link;

            //redirecting to the payment_link
            _httpContextAccessor.HttpContext.Response.Redirect(payment_link);
        }

       
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });

        }

        public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            dynamic amount = refundPaymentRequest.AmountToRefund;
            if (refundPaymentRequest.IsPartialRefund == true)
            {
                 amount = refundPaymentRequest.AmountToRefund != refundPaymentRequest.Order.OrderTotal && refundPaymentRequest.AmountToRefund <= refundPaymentRequest.Order.OrderTotal
                    ? (decimal?)refundPaymentRequest.AmountToRefund
                    : null;
            }          

            //get the primary store currency
            var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId)
                ?? throw new NopException("Primary store currency cannot be loaded");

            var order_id = refundPaymentRequest.Order.Id.ToString();
                       
            HttpRequestMessage request;
            HttpResponseMessage response;

            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://sandbox.cashfree.com/pg/orders/"+order_id+"/refunds" :
                "https://api.cashfree.com/pg/orders/" +order_id +"/refunds";

            string responsebody;
            var client = new HttpClient();
            request = new HttpRequestMessage(HttpMethod.Post, url);
            var stringdata = JsonConvert.SerializeObject(new RefundModel()
            {
                refund_id = "refund_"+order_id,
                refund_amount = Convert.ToDouble(amount),
                refund_note = "refund of" + order_id

            });

            var stringcontent = new StringContent(stringdata, Encoding.UTF8, "application/json");
            request.Content = stringcontent;
            var listheaders = new List<NameValueHeaderValue>
            {
                new NameValueHeaderValue("x-api-version", _cashfreePaymentSettings.ApiVersion),
                new NameValueHeaderValue("x-client-id", _cashfreePaymentSettings.AppID),
                new NameValueHeaderValue("x-client-secret", _cashfreePaymentSettings.SecretKey)
            };
            foreach (var header in listheaders)
            {
                request.Headers.Add(header.Name, header.Value);
            }
            response = await client.SendAsync(request);
            responsebody = await response.Content.ReadAsStringAsync();
            if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest" || response.ReasonPhrase == "400")
            {
                throw new Exception(await _localizationService.GetResourceAsync(responsebody));
            }
            dynamic result = JsonConvert.DeserializeObject<RefundModel>(responsebody);
            var refundStatus = result.refund_status;
            var payment_status = GetPaymentStatus(refundStatus);
            return new RefundPaymentResult
            {
                NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : payment_status
            };

        }

        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }
        
        public static PaymentStatus GetPaymentStatus(string paymentStatus)
        {
            var result = PaymentStatus.Pending;

            if (paymentStatus == null)
                paymentStatus = string.Empty;

            switch (paymentStatus.ToUpperInvariant())
            {
                
                case "PENDING":
                    result = PaymentStatus.Pending;
                    break;
                case "SUCCESS":
                    result = PaymentStatus.Refunded;
                    break;
                case "ONHOLD":
                    result = PaymentStatus.Pending;
                    break;
                case "CANCELLED":
                    result = PaymentStatus.Voided;
                    break;
                default:
                    break;
            }

            switch (paymentStatus.ToLowerInvariant())
            {
                case "pending":
                    result = PaymentStatus.Pending;
                    break;
                case "success":
                    result = PaymentStatus.Refunded;
                    break;
                case "onhold":
                    result = PaymentStatus.Pending;
                    break;
                case "cancelled":
                    result = PaymentStatus.Voided;
                    break;
                default:
                    break;
            }

            return result;
        }
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentCashfree/Configure";
        }
    }
}