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

        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //return Task.FromResult(new CancelRecurringPaymentResult());
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
                
                }
            
            });
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

            return await Task.FromResult(result);

        }

        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            
            if (_cashfreePaymentSettings.PaymentType != 0)////AuthorizeAndCapture = 0 ,AuthorizeOnly = 1
            {
                //-------------CREATE ORDER------------
                //////
                //////
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
                    order_note = "order:" + order.Id,
                    customer_details = new CustomerDetails()
                    {
                        customer_id = customer.Id.ToString(),
                        customer_name = address.FirstName,
                        customer_email = "fathima.p@manprax.com",
                        customer_phone = address.PhoneNumber,
                        customer_bank_account_number = "1234567890",
                        customer_bank_ifsc = "HDFC0000102"
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


                //-----------PAYORDER---------------
                ///////
                ///////

                HttpRequestMessage payOrderRequest;
                HttpResponseMessage payOrderResponse;
                string payOrderResponseBody;
                var client2 = new HttpClient();
                var payOrderUrl = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                    "https://sandbox.cashfree.com/pg/orders/pay" :
                    "https://api.cashfree.com/pg/orders/pay";

                payOrderRequest = new HttpRequestMessage(HttpMethod.Post, payOrderUrl);
                var stringdata2 = string.Empty;
                var cardNumber = string.Empty;
                var cardName = string.Empty;
                var expireYear = string.Empty;
                var cardExpireYear = string.Empty;
                var cardExpireMonth = string.Empty;
                var cardCvv = string.Empty;

                //Decryting the card informations from paymentInfo
                if (postProcessPaymentRequest.Order.AllowStoringCreditCardNumber)
                {
                    cardNumber = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardNumber);
                    cardName = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardName);
                    cardExpireMonth = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationMonth);
                    cardCvv = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardCvv2);
                    expireYear = _encryptionService.DecryptText(postProcessPaymentRequest.Order.CardExpirationYear);
                    cardExpireYear = expireYear.Substring(expireYear.Length - 2);
                }

                //getting UpiId stored in the custom values
                var doc = new XmlDocument();
                doc.LoadXml(postProcessPaymentRequest.Order.CustomValuesXml);
                XmlNodeList nodeList = doc.GetElementsByTagName("key");
                var upiId = string.Empty;
                foreach (XmlNode node in nodeList)
                {
                    upiId = node.InnerText;
                }

                //checking upiid is null
                if (string.IsNullOrEmpty(upiId))
                {
                    //card data model for API
                    stringdata2 = JsonConvert.SerializeObject(new PayOrderCardModel()
                    {
                        order_token = result.order_token,
                        //save_instrument = true,
                        payment_method = new PaymentMethodCard()
                        {
                            card = new CardPaymentMethod()
                            {

                                channel = "link", //or link
                                card_cvv = cardCvv,
                                card_expiry_mm = cardExpireMonth,
                                card_expiry_yy = cardExpireYear,
                                card_holder_name = cardName,
                                card_number = cardNumber

                            }
                        },
                    });

                }
                else
                {
                    //upi data model for API
                    stringdata2 = JsonConvert.SerializeObject(new PayOrderUpiModel()
                    {
                        order_token = result.order_token,
                        payment_method = new PaymentMethodUpi()
                        {
                            upi = new UpiPaymentMethod()
                            {

                                upi_id = upiId,//"testsuccess@gocash",
                                authorize_only = false,
                                channel = "collect",
                                authorization = new UPIAuthorizedDetails()
                                {
                                    approve_by = (DateTime.Now.AddHours(5)).ToString("o"),
                                    start_time = (DateTime.Now.AddDays(1)).ToString("o"),
                                    end_time = (DateTime.Now.AddDays(2)).ToString("o") 
                                }

                            }
                        },
                    });
                    
                }

                var stringcontent2 = new StringContent(stringdata2, Encoding.UTF8, "application/json");
                payOrderRequest.Content = stringcontent2;
                var listheaders2 = new List<NameValueHeaderValue>
                {
                    new NameValueHeaderValue("x-api-version", _cashfreePaymentSettings.ApiVersion),
                    new NameValueHeaderValue("x-client-id", _cashfreePaymentSettings.AppID),
                    new NameValueHeaderValue("x-client-secret", _cashfreePaymentSettings.SecretKey)
                };
                foreach (var header in listheaders2)
                {
                    payOrderRequest.Headers.Add(header.Name, header.Value);
                }

                payOrderResponse = await client2.SendAsync(payOrderRequest);
                payOrderResponseBody = await payOrderResponse.Content.ReadAsStringAsync();
                dynamic payOrderResult = JsonConvert.DeserializeObject<ResponseModel>(payOrderResponseBody);


                if (payOrderResponse.ReasonPhrase == "Bad Request" || payOrderResponse.ReasonPhrase == "BadRequest")
                {
                    throw new Exception(await _localizationService.GetResourceAsync(payOrderResponseBody));

                }

                var paymentstatus = PaymentStatus.Authorized;
                await ProcessPaymentstatusAsync(Convert.ToInt32(order.Id), paymentstatus, payOrderResult.cf_payment_id);
                
                await Task.CompletedTask;
            }
            else
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
                    order_note = "order:" + order.Id,
                    customer_details = new CustomerDetails()
                    {
                        customer_id = customer.Id.ToString(),
                        customer_name = address.FirstName,
                        customer_email = customer.Email,
                        customer_phone = address.PhoneNumber,
                        customer_bank_account_number = "1234567890",
                        customer_bank_ifsc = "HDFC0000102"
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
    }
}