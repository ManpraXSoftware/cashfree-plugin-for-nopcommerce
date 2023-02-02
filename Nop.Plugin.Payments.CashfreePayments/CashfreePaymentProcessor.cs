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
using Nop.Services.Logging;
using System.Xml;
using Nop.Services.Security;
using Nop.Plugin.Payments.CashfreePayments.Components;

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
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IEncryptionService _encryptionService;

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
            IOrderService orderService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IEncryptionService encryptionService)
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
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _encryptionService = encryptionService;
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
                ["plugins.payments.cashfreepayments.fields.paymentmethods.upi"] = "upi -> to pay using upi",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.nb"] = "nb ->to pay through netbanking ",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.cc"] = "cc -> to pay through cedit card",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.dc"] = "dc -> to pay using debit card",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.paylater"] = "paylater -> to pay through paylater providers",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.emi"] = "emi -> to pay through emi",
                ["plugins.payments.cashfreepayments.fields.paymentmethods.description.instructions"] ="{For example, if you want to accept only netbanking and UPI from customers, you must pass the following value 'nb,upi' }",
                ["Plugins.Payments.CashfreePayments.Fields.Instructions"] = @"
                    <p>
                        <b>If you're using this gateway ensure that your primary store currency is supported by Cashfree.</b>	                    
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
         public bool SupportCapture => true;
        public bool SupportPartiallyRefund => true;
        public bool SupportRefund => true;
        public bool SupportVoid => true;
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => false;

        public async Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var subReferenceId = cancelPaymentRequest.Order.SubscriptionTransactionId;
            //return Task.FromResult(new CancelRecurringPaymentResult());
            
            HttpRequestMessage request;
            HttpResponseMessage response;
            string responsebody;
            var client = new HttpClient();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://test.cashfree.com/api/v2/subscriptions/" + subReferenceId + "/cancel" :
                "https://api.cashfree.com/api/v2/subscriptions/" + subReferenceId + "/cancel";

            request = new HttpRequestMessage(HttpMethod.Post, url);
           

            var stringcontent = new StringContent("", Encoding.UTF8, "application/json");
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
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);
            if (result.status != "OK")
            {
                return await Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Subscription Cancelling failed" } });
            }

            //always success
            return await Task.FromResult(new CancelRecurringPaymentResult());

        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>

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
                amount = Convert.ToDouble(capturePaymentRequest.Order.OrderTotal)
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
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);

            if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest" || response.ReasonPhrase == "400")
            {
                throw new Exception(await _localizationService.GetResourceAsync(responsebody));

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
            if (_cashfreePaymentSettings.PaymentType == 0)////AuthorizeAndCapture = 0 ,AuthorizeOnly = 1
            {
                return Task.FromResult(new ProcessPaymentRequest());
            }
            string upi = form["UpiId"];
            string creditCardType = form["CreditCardType"];
            string creditCardName = form["CreditCardName"];
            string creditCardNumber = form["CreditCardNumber"];
            string creditCardExpireMonth = form["CreditCardExpireMonth"];
            string creditCardExpireYear = form["CreditCardExpireYear"];
            string creditCardCvv2 = form["CreditCardCvv2"];

            return Task.FromResult(new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"],
                CustomValues = new Dictionary<string, object> {
                    { upi, new { upi = upi } }
                //    { creditCardName, new { creditCardName = creditCardName } } ,
                //{ creditCardNumber, new { creditCardNumber = creditCardNumber } },
                //{ creditCardExpireYear, new { creditCardExpireYear = creditCardExpireYear } },
                //{ creditCardExpireMonth, new { creditCardExpireMonth = creditCardExpireMonth } },
                //    { creditCardCvv2, new { creditCardCvv2 = creditCardCvv2 } }
                }
            
            });
        }

        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.CashfreePayments.PaymentMethodDescription");

        }

        

        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }       

       
        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (_cashfreePaymentSettings.PaymentType == 0)////AuthorizeAndCapture = 0 ,AuthorizeOnly = 1
            {
                return await Task.FromResult(new ProcessPaymentResult()
                {
                });
            }


            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = true
            };
            ////return Task.FromResult(result);
            //dynamic upiID = "";
            //if (processPaymentRequest.CustomValues != null)
            //{

            //    foreach (var i in processPaymentRequest.CustomValues)
            //    {
            //        upiID = i.Key.ToString();


            //    }
            //}
            //    var p = new PostProcessPaymentRequest();
            //    p.Order.CustomValuesXml = upiID;
            //p.Order.CardCvv2 = processPaymentRequest.CreditCardCvv2;
            //p.Order.CardExpirationMonth = processPaymentRequest.CreditCardExpireMonth.ToString();
            //p.Order.CardExpirationYear = processPaymentRequest.CreditCardExpireYear.ToString();
            //p.Order.CardName = processPaymentRequest.CreditCardName;
            //p.Order.CardNumber = processPaymentRequest.CreditCardNumber;

            return await Task.FromResult(result);

        }

        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if(!string.IsNullOrEmpty(postProcessPaymentRequest.Order.SubscriptionTransactionId))
            {
                _httpContextAccessor.HttpContext.Response.Redirect(postProcessPaymentRequest.Order.AuthorizationTransactionResult);

            }
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
                        order_currency =order.CustomerCurrencyCode.ToString(),
                        order_note = "order_id_" + order.Id ,
                        customer_details = new CustomerDetails()
                        {
                            customer_id = customer.Id.ToString(),
                            customer_name = address.FirstName.ToString(),
                            customer_email = address.Email.ToString(),
                            customer_phone = address.PhoneNumber.ToString()
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
                    dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);

                    if (response.ReasonPhrase == "Bad Request" || response.ReasonPhrase == "BadRequest")
                    {
                        throw new Exception(await _localizationService.GetResourceAsync(responsebody));

                    }
                    var payment_link = result.payment_link;

                    _httpContextAccessor.HttpContext.Response.Redirect(payment_link);               
                        
            }

        protected  async Task ProcessPaymentstatusAsync(int orderId, PaymentStatus newPaymentStatus, string cf_order_id)
        {

            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                await _logger.ErrorAsync("Cashfree Order is not found");
                return;
            }

            //order note
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = orderId,
                Note = $"Order status has been changed to{newPaymentStatus}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            switch (newPaymentStatus)
            {
                case PaymentStatus.Authorized:
                    if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                        await _orderProcessingService.MarkAsAuthorizedAsync(order);
                    break;
                case PaymentStatus.Paid:
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = cf_order_id;
                        await _orderService.UpdateOrderAsync(order);

                        await _orderProcessingService.MarkOrderAsPaidAsync(order);
                    }

                    break;
                case PaymentStatus.Voided:
                    if (_orderProcessingService.CanVoidOffline(order))
                        await _orderProcessingService.VoidOfflineAsync(order);

                    break;
            }

        }
        public async Task<dynamic> CreatePlanAsync(ProcessPaymentRequest processPaymentRequest)
        {
            HttpRequestMessage request;
            HttpResponseMessage response;
            string responsebody;
            var client = new HttpClient();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://test.cashfree.com/api/v2/subscription-plans" :
                "https://api.cashfree.com/api/v2/subscription-plans";
            var planid = "";

            var str = DateTime.Now.ToString();
            foreach (char c in str) 
            {
                  if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') )
                  {
                    planid += c.ToString(); //planId should n't contain any special characters
                  }
            }
            string intervalType ="";
            switch ((processPaymentRequest.RecurringCyclePeriod).ToString())
            {
                case "Years":
                    intervalType = "year";
                    break;

                case "Months":
                    intervalType = "month";
                    break;

                case "Weeks":
                    intervalType = "week";
                    break;

                case "Days":
                    intervalType = "day";
                    break;
            }
            request = new HttpRequestMessage(HttpMethod.Post, url);
            var subPlanModel = new SubscriptionPlanModel()
            {
                planId = "Basic-" +planid.Trim(),
                planName = "Basic Subscription Plan" + planid,
                type = "PERIODIC", //type = PERIODIC OR ON_DEMAND
                maxCycles = processPaymentRequest.RecurringTotalCycles,
                amount = 1000,
                intervalType = intervalType,
                intervals = 1,
                description = "standard plan"
            };
            var stringdata = JsonConvert.SerializeObject(subPlanModel);

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
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);
            if(result.status != "OK")
            {
                throw new Exception(await _localizationService.GetResourceAsync(result.message));
            }
            return subPlanModel.planId;
        }

        public async Task<dynamic> CreateSubscriptionAsync(ProcessPaymentRequest processPaymentRequest)
        {
            //get current store
            var store = await _storeContext.GetCurrentStoreAsync();

            //get store location
            var storeLocation = _webHelper.GetStoreLocation();

            //get current customer
            var customer = await _workContext.GetCurrentCustomerAsync();

            //get current order details
            var order = (await _orderService.SearchOrdersAsync(store.Id,
                customerId: customer.Id, pageSize: 1)).FirstOrDefault();

            //get address of customer
            var address = await _addressService.GetAddressByIdAsync(Convert.ToInt32(customer.BillingAddressId));


            HttpRequestMessage request;
            HttpResponseMessage response;
            string responsebody;

            var planId = await CreatePlanAsync(processPaymentRequest);
            string firstDate = "2022-12-12";//DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");
            var dateWithoutT = "";
            var dt = DateTime.Now.AddMonths(13).ToString("yyyy-MM-dd HH:mm:ss");
            foreach (char c in dt)
            {
                if (c != 'T')
                {
                    dateWithoutT += c.ToString(); //planId should n't contain any special characters
                }
            }

            var subId = "";

            var str = DateTime.Now.ToString();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    subId += c.ToString(); //SubstractionId should n't contain any special characters
                }
            }
            var client = new HttpClient();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                "https://test.cashfree.com/api/v2/subscriptions" :
                "https://api.cashfree.com/api/v2/subscriptions";

            request = new HttpRequestMessage(HttpMethod.Post, url);
            var stringdata = JsonConvert.SerializeObject(new SubscriptionModel()
            {
                planId = planId,
                subscriptionId = "SUB-" + subId.Trim(),
                customerName = address.FirstName,
                customerEmail = customer.Email,
                customerPhone = address.PhoneNumber,
               // firstChargeDate = DateTime.ParseExact(firstDate, "yyyy-MM-dd", null), //format =2022-11-10
                authAmount = 1,
                //expiresOn = DateTime.ParseExact("2023-12-12 11:11:11", "yyyy-MM-dd HH:mm:ss",null), //format = 2023-12-12 11:11:11
                returnUrl = $"{storeLocation}Plugins/PaymentCashfree/SubscriptionReturnUrl",
                subscriptionNote = "",
                notificationChannels = new string[] { "EMAIL", "SMS" }
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
            dynamic result = JsonConvert.DeserializeObject<ResponseModel>(responsebody);            
            return result;
            //we need to store subReferenceId from the result
        }
        public async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
           
           ResponseModel subs = await CreateSubscriptionAsync(processPaymentRequest);

            var subReferenceId = subs.subReferenceId;
            

           //_httpContextAccessor.HttpContext.Response.Redirect(subs.authLink);

            if (!string.IsNullOrEmpty(subReferenceId))
            {
                return await Task.FromResult(new ProcessPaymentResult
                {
                    SubscriptionTransactionId = subReferenceId,
                    AllowStoringCreditCardNumber = true,
                    AuthorizationTransactionResult = subs.authLink
                    
                });
            }
            return await Task.FromResult(new ProcessPaymentResult());
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
            if (_cashfreePaymentSettings.PaymentType == 0)////AuthorizeAndCapture = 0 ,AuthorizeOnly = 1
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }
            var warnings = new List<string>();

            //validate
            // var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"], 
                UpiId = form["UpiId"]
            };
           // var validationResult = validator.Validate(model);
            //if (!validationResult.IsValid)
            //    warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return Task.FromResult<IList<string>>(warnings);
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

        public Type GetPublicViewComponent()
        {
            return typeof(PaymentCashfreeViewComponent);
        }
    }
}