using System;
namespace Nop.Plugin.Payments.CashfreePayments
{
    /// <summary>
    /// Represents payment type enumeration
    /// </summary>
    public enum SubscriptionStatus
    {
        ///<summary>
        /// PENDING - initially set to SubscriptionStatus
        ///</summary>
        PENDING = 10,

        /// <summary>
        /// INITIALIZED - The subscription has just been created and is ready to be authorized by the customer.
        /// </summary>
        INITIALIZED = 0,

        /// <summary>
        /// BANK_APPROVAL_PENDING - E-Mandate has been authorised and registration is pending at the Bank. This status is specific for e-mandates.
        /// </summary>
        BANK_APPROVAL_PENDING = 1,

        /// <summary>
        /// ACTIVE - The customer has authorized the subscription and will be debited accordingly.
        /// </summary>
        ACTIVE = 2,

        /// <summary>
        /// ON_HOLD - The subscription failed due to insufficient funds, expiration of payment method, and so on.
        /// </summary>
        ON_HOLD = 3,

        /// <summary>
        /// CANCELLED - The subscription was cancelled by the merchant and can no longer be activated.
        /// </summary>
        CANCELLED = 4,

        /// <summary>
        /// COMPLETED - The subscription has completed its total active period.
        /// </summary>
        COMPLETED = 5
    }
}
