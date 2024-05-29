﻿namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

public class Constants
{
    public const string DimocoMerchantIdLiveProperty = "dimocoMerchantIdLive";
    public const string DimocoMerchantIdTestProperty = "dimocoMerchantIdTest";
    public const string DimocoRequestIdProperty = "dimocoRequestId";

    public const string StartPaymentTransactionAction = "start";
    public const string OrderIdsParameterName = "cp_order_ids";
    public const string InvoiceNumberParameterName = "cp_invoice_number";

    public const string WebhookDataProperty = "data";
    public const string WebhookSignatureProperty = "digest";
}