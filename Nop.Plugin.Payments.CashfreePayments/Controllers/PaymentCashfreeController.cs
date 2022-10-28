using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CashfreePayments.Models;
using Nop.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CashfreePayments.Controllers
{
    public class PaymentCashfreeController : BasePaymentController
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IAddressService _addressService;
        private readonly CashfreePaymentSettings _cashfreePaymentSettings;
        private readonly HttpClient _httpClient;
        private readonly ICustomerService _customerService;
        private readonly OrderSettings _orderSettings;

        #endregion
        public PaymentCashfreeController(IGenericAttributeService genericAttributeService,
                IOrderProcessingService orderProcessingService,
                IOrderService orderService,
                IPaymentPluginManager paymentPluginManager,
                IPermissionService permissionService,
                ILocalizationService localizationService,
                ILogger logger,
                INotificationService notificationService,
                ISettingService settingService,
                IStoreContext storeContext,
                IWebHelper webHelper,
                IWorkContext workContext,
                ShoppingCartSettings shoppingCartSettings,
                IAddressService addressService,
                CashfreePaymentSettings cashfreePaymentSettings,
                HttpClient httpClient,
                ICustomerService customerService,
                OrderSettings orderSettings)
        {
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _paymentPluginManager = paymentPluginManager;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _logger = logger;
            _notificationService = notificationService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _shoppingCartSettings = shoppingCartSettings;
            _addressService = addressService;
            _cashfreePaymentSettings = cashfreePaymentSettings;
            _httpClient = httpClient;
            _customerService = customerService;
            _orderSettings = orderSettings;
        }


        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var cashfreePaymentSettings = await _settingService.LoadSettingAsync<CashfreePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                Title = cashfreePaymentSettings.Title,
                AppID = cashfreePaymentSettings.AppID,
                SecretKey = cashfreePaymentSettings.SecretKey,
                Description = cashfreePaymentSettings.Description,
                ActiveEnvironmentId = Convert.ToInt32(cashfreePaymentSettings.ActiveEnvironment),
                PaymentMethods = cashfreePaymentSettings.PaymentMethods,
                ApiVersion = cashfreePaymentSettings.ApiVersion,
                ActiveEnvironmentValues = await cashfreePaymentSettings.ActiveEnvironment.ToSelectListAsync(),
                PaymentTypeId = Convert.ToInt32(cashfreePaymentSettings.PaymentType),
                PaymentTypeValues = await cashfreePaymentSettings.PaymentType.ToSelectListAsync(),
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.CashfreePayments/Views/Configure.cshtml", model);

            model.Title_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.Title, storeScope);
            model.AppID_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.AppID, storeScope);
            model.SecretKey_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.SecretKey, storeScope);
            model.Description_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.Description, storeScope);
            model.ActiveEnvironmentId_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.ActiveEnvironment, storeScope);
            model.PaymentMethods_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.PaymentMethods, storeScope);
            model.ApiVersion_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.ApiVersion, storeScope);
            model.PaymentTypeId_OverrideForStore = await _settingService.SettingExistsAsync(cashfreePaymentSettings, x => x.PaymentType, storeScope);

            return View("~/Plugins/Payments.CashfreePayments/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var cashfreePaymentSettings = await _settingService.LoadSettingAsync<CashfreePaymentSettings>(storeScope);

            //save settings
            cashfreePaymentSettings.Title = model.Title;
            cashfreePaymentSettings.AppID = model.AppID;
            cashfreePaymentSettings.SecretKey = model.SecretKey;
            cashfreePaymentSettings.Description = model.Description;
            cashfreePaymentSettings.ActiveEnvironment = (ActiveEnvironment)model.ActiveEnvironmentId;
            cashfreePaymentSettings.PaymentMethods = model.PaymentMethods;
            cashfreePaymentSettings.PaymentType = (PaymentType)model.PaymentTypeId;
            cashfreePaymentSettings.ApiVersion=model.ApiVersion;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.Title, model.Title_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.AppID, model.AppID_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.SecretKey, model.SecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.Description, model.Description_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.ActiveEnvironment, model.ActiveEnvironmentId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.PaymentType, model.PaymentTypeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.PaymentMethods, model.PaymentMethods_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(cashfreePaymentSettings, x => x.ApiVersion, model.ApiVersion_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        //return_url
        public async Task<IActionResult> HandleResponse(string order_id,string order_token)
        {
            //get current customer
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (await _customerService.IsGuestAsync(customer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();


            //get the order by order_id
            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(order_id));
            if (order == null)
                return RedirectToRoute("Homepage");

            var client = new HttpClient();
            var url = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                new Uri("https://sandbox.cashfree.com/pg/orders/" + order_id) :
               new Uri("https://api.cashfree.com/pg/orders/" + order_id);

            client.DefaultRequestHeaders.Add("x-api-version", _cashfreePaymentSettings.ApiVersion);
            client.DefaultRequestHeaders.Add("x-client-id", _cashfreePaymentSettings.AppID);
            client.DefaultRequestHeaders.Add("x-client-secret", _cashfreePaymentSettings.SecretKey);

            //get the order details from cashfree
            var result =await client.GetAsync(url);
            var json = result.Content.ReadAsStringAsync().Result;
            dynamic result2 = JsonConvert.DeserializeObject(json);           
            string order_status = result2.order_status;//The order status -ACTIVE, PAID, EXPIRED
            string cf_order_id=result2.cf_order_id;
            var paymentstatus = PaymentStatus.Pending;

            //get payment details of order
            var client2 = new HttpClient();
            client2.DefaultRequestHeaders.Add("x-api-version", _cashfreePaymentSettings.ApiVersion);
            client2.DefaultRequestHeaders.Add("x-client-id", _cashfreePaymentSettings.AppID);
            client2.DefaultRequestHeaders.Add("x-client-secret", _cashfreePaymentSettings.SecretKey);
            //var paymentLink = result2.payments.url;
            var paymentUrl = _cashfreePaymentSettings.ActiveEnvironment == 0 ?
                new Uri("https://sandbox.cashfree.com/pg/orders/" + order_id + "/payments") :
               new Uri("https://api.cashfree.com/pg/orders/" + order_id + "/payments");
            var paymentResult = await client2.GetAsync(paymentUrl);
            var paymentJson = paymentResult.Content.ReadAsStringAsync().Result;
            dynamic paymentResult2 = JsonConvert.DeserializeObject(paymentJson);
            //string a = paymentResult2.cf_payment_id;
            //string isCaptured = paymentResult2.is_captured;
            //string paymentStatus = paymentResult2.payment_status;
            //var paymentstat = PaymentStatus.Pending;
            //var paystatus = GetPaymentStatus(paymentStatus);
            ///payment status ==SUCCESS,FAILED,MOT-ATTEMPTED,PENDING,FLAGGED,CAMCELLED,VOID,USER-DROPPED
           

            if (order_status == "ACTIVE" || order_status == "active")
            {
                //update payment_status=pending when cancel the transaction
                if (order != null)
                    return RedirectToRoute("OrderDetails", new { orderId = order.Id });

                return RedirectToRoute("Homepage");
            }
            else if (order_status == "EXPIRED" || order_status == "expired")
            {
                //cancel order
                await _orderProcessingService.CancelOrderAsync(order, true);
                return RedirectToRoute("Homepage");

            }
            else if (order_status == "PAID"  )
            {

                // paymentstatus = paystatus;
                paymentstatus = PaymentStatus.Paid;
                 //updating ordernote and payment_status in Order
                 await ProcessPaymentAsync(order.Id, paymentstatus, cf_order_id);
            }        
           

            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }      
        
        private string ComputeSignature(string secret,string timestamp,string strRequest)
        {         
            var body = timestamp + strRequest;
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(body);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }         

        }
        public async Task<IActionResult> NotifyUrl() 
        {
            //3 steps to verify webhooks:-
            //Get the payload from the webhook endpoint.
            // Generate the signature.
            //Verify the signature

            //Get the payload from the webhook endpoint.
            await using var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);
            var strRequest = Encoding.ASCII.GetString(stream.ToArray());

            // Generate the signature.
            var secretkey = _cashfreePaymentSettings.SecretKey;

            var timestamp = Request.Headers["x-webhook-timestamp"];
            var signature = Request.Headers["x-webhook-signature"];
            var generatedSignature = ComputeSignature(secretkey,timestamp,strRequest);

            //verify the signature
            if (signature != generatedSignature)
            {
                return BadRequest("Failure in verifying signature");
            }
           

            dynamic result = JsonConvert.DeserializeObject(strRequest);  
            var payment_status = result.data.payment.payment_status;
            var payment_message = result.data.payment.payment_message;
            var type = result.type;
            var order_id = result.data.order.order_id;
            var cf_payment_id = result.cf_payment_id;

            //get the order by order_id
            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(order_id));

            if (order == null)
                return Ok();

            var paymentStatus = PaymentStatus.Pending;
            if (payment_status == "FAILED" || payment_status == "failed" || payment_status == "USER_DROPPED" || payment_status == "user_dropped")
            {
                
                //USER_DROPPED==drop out of the payment flow without completing the transaction
                //cancel order
                await _orderProcessingService.CancelOrderAsync(order, true);
               
            }
           
            else if (payment_status == "SUCCESS" || payment_status == "success")
            {
                paymentStatus = PaymentStatus.Paid;
                await ProcessPaymentAsync(Convert.ToInt32(order_id), paymentStatus, cf_payment_id);
            }     
            return Ok();           
        }
        

        protected virtual async Task ProcessPaymentAsync(int orderId, PaymentStatus newPaymentStatus,string cf_order_id)
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
                Note = $"Order status has been changed to{ newPaymentStatus }",
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
                    result = PaymentStatus.Paid;
                    break;
                case "FAILED":
                  //  result = PaymentStatus.Voided;
                    break;
                case "CANCELLED":
                  //  result = PaymentStatus.Voided;
                    break;               
                case "FLAGGED":
                   // result = PaymentStatus.Voided;
                    break;
                case "VOID":
                    result = PaymentStatus.Voided;
                    break;
                case "USER_DROPPED":
                  //  result = PaymentStatus.Voided;
                    break;
                case "NOT_ATTEMPTED":
                   // result = PaymentStatus.Voided;
                    break;
                default:
                    break;
            }

            return result;
        }

        #endregion
    }
}
