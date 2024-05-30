using System.Data;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Payments.Dimoco.Models;
using GeeksCoreLibrary.Modules.Payments.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using OrderProcessConstants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;
using DimocoConstants = GeeksCoreLibrary.Modules.Payments.Dimoco.Models.Constants;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Services;

/// <inheritdoc cref="IPaymentServiceProviderService" />
public class DimocoService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
{
    private const string BaseUrl = "https://services.dimoco.at/smart/payment";
    private readonly IDatabaseConnection databaseConnection;
    private readonly ILogger<PaymentServiceProviderBaseService> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GclSettings gclSettings;
    private readonly IShoppingBasketsService shoppingBasketsService;
    private readonly IWiserItemsService wiserItemsService;
    private readonly IObjectsService objectsService;

    private string webHookContents = null;
    private ApiResultModel webhookData = null;
    private string? invoiceNumber = null;

    public DimocoService(
        IDatabaseHelpersService databaseHelpersService,
        IDatabaseConnection databaseConnection,
        ILogger<PaymentServiceProviderBaseService> logger,
        IOptions<GclSettings> gclSettings,
        IShoppingBasketsService shoppingBasketsService,
        IWiserItemsService wiserItemsService,
        IObjectsService objectsService,
        IHttpContextAccessor httpContextAccessor = null) : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
    {
        this.databaseConnection = databaseConnection;
        this.logger = logger;
        this.shoppingBasketsService = shoppingBasketsService;
        this.wiserItemsService = wiserItemsService;
        this.objectsService = objectsService;
        this.gclSettings = gclSettings.Value;
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
    {
        var dimocoSettings = (DimocoSettingsModel) paymentMethodSettings.PaymentServiceProvider;
        var status = 0;
        var requestFormValues = "";
        var error = "";
        var responseBody = "";

        try
        {
            var invalidSettings = new List<string>();
            if (String.IsNullOrWhiteSpace(dimocoSettings.MerchantId))
            {
                invalidSettings.Add(nameof(dimocoSettings.MerchantId));
            }

            if (String.IsNullOrWhiteSpace(dimocoSettings.OrderId))
            {
                invalidSettings.Add(nameof(dimocoSettings.OrderId));
            }

            if (String.IsNullOrWhiteSpace(dimocoSettings.ClientSecret))
            {
                invalidSettings.Add(nameof(dimocoSettings.ClientSecret));
            }

            if (invalidSettings.Any())
            {
                logger.LogError($"Validation in '{nameof(HandlePaymentRequestAsync)}' of '{nameof(DimocoService)}' failed because the following required settings do not have a (valid) value: {String.Join(", ", invalidSettings)}.");
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = dimocoSettings.FailUrl
                };
            }


            var totalPrice = 0M;
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            foreach (var conceptOrder in conceptOrders)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(conceptOrder.Main, conceptOrder.Lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            // Collect some data we need.
            var firstOrder = conceptOrders.First();
            var requestId = Guid.NewGuid().ToString();
            var emailAddress = firstOrder.Main.GetDetailValue(OrderProcessConstants.EmailAddressProperty);
            var phoneNumber = firstOrder.Main.GetDetailValue(OrderProcessConstants.PhoneNumberProperty);
            var languageCode = firstOrder.Main.GetDetailValue(OrderProcessConstants.LanguageCodeProperty);
            var countryCode = firstOrder.Main.GetDetailValue(OrderProcessConstants.CountryCodeProperty);

            // Build and execute payment request.
            var restClient = new RestClient(new RestClientOptions(BaseUrl));
            var restRequest = new RestRequest("", Method.Post);

            restRequest.AddParameter("merchant", dimocoSettings.MerchantId, ParameterType.GetOrPost);
            restRequest.AddParameter("order", dimocoSettings.OrderId, ParameterType.GetOrPost);
            restRequest.AddParameter("action", DimocoConstants.StartPaymentTransactionAction, ParameterType.GetOrPost);
            restRequest.AddParameter("request_id", requestId, ParameterType.GetOrPost);
            restRequest.AddParameter("url_callback", dimocoSettings.WebhookUrl, ParameterType.GetOrPost);
            restRequest.AddParameter("url_return", dimocoSettings.SuccessUrl, ParameterType.GetOrPost);
            restRequest.AddParameter("service_name", dimocoSettings.ServiceName, ParameterType.GetOrPost);
            restRequest.AddParameter("amount", totalPrice.ToString(new CultureInfo("en-US")), ParameterType.GetOrPost);
            restRequest.AddParameter(DimocoConstants.OrderIdsParameterName, String.Join(",", conceptOrders.Select(o => o.Main.Id)), ParameterType.GetOrPost);
            restRequest.AddParameter(DimocoConstants.InvoiceNumberParameterName, invoiceNumber, ParameterType.GetOrPost);

            if (!String.IsNullOrWhiteSpace(emailAddress))
            {
                restRequest.AddParameter("shopper", emailAddress, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(phoneNumber))
            {
                restRequest.AddParameter("msisdn", phoneNumber, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(languageCode))
            {
                restRequest.AddParameter("language", languageCode, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(countryCode))
            {
                restRequest.AddParameter("country", countryCode, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(dimocoSettings.LogoUrl))
            {
                var merchantArguments = new MerchantArgumentsModel
                {
                    Logo = new PictureModel
                    {
                        Url = dimocoSettings.LogoUrl,
                        AltText = dimocoSettings.ServiceName
                    }
                };

                restRequest.AddParameter("prompt_merchant_args", JsonConvert.SerializeObject(merchantArguments), ParameterType.GetOrPost);
            }

            var products = shoppingBasketsService.GetLines(firstOrder.Lines, OrderProcessConstants.OrderLineProductType);
            var firstProductWithImage = products.FirstOrDefault(p => !String.IsNullOrWhiteSpace(p.GetDetailValue(OrderProcessConstants.ImageUrlProperty)));
            if (firstProductWithImage != null)
            {
                var productDescription = firstProductWithImage.GetDetailValue(OrderProcessConstants.DescriptionProperty);
                var productArguments = new ProductArgumentsModel
                {
                    Picture = new PictureModel
                    {
                        Url = firstProductWithImage.GetDetailValue(OrderProcessConstants.ImageUrlProperty),
                        AltText = productDescription
                    },
                    Description = new Dictionary<string, string>
                    {
                        {languageCode, productDescription}
                    }
                };

                restRequest.AddParameter("prompt_product_args", JsonConvert.SerializeObject(productArguments), ParameterType.GetOrPost);
            }

            var payload = String.Join("", restRequest.Parameters.Where(p => p.Type == ParameterType.GetOrPost).OrderBy(p => p.Name).Select(p => p.Value));
            using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(dimocoSettings.ClientSecret));
            var signature = Convert.ToHexString(hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            restRequest.AddParameter("digest", signature, ParameterType.GetOrPost);

            requestFormValues = String.Join(Environment.NewLine, restRequest.Parameters.Where(p => p.Type == ParameterType.GetOrPost).Select(p => $"{p.Name}: {p.Value}"));

            var restResponse = await restClient.ExecuteAsync(restRequest);
            responseBody = restResponse.Content;
            status = (int)restResponse.StatusCode;

            if (restResponse.StatusCode != HttpStatusCode.OK && restResponse.StatusCode != HttpStatusCode.Created && restResponse.StatusCode != HttpStatusCode.NoContent)
            {
                logger.LogError($"Starting a payment transaction with Dimoco failed with HTTP status code {status}.");
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = dimocoSettings.FailUrl
                };
            }

            var actionResponse = XmlHelpers.DeserializeXml<ApiResultModel>(responseBody);
            if (actionResponse?.ActionResult == null)
            {
                logger.LogError($"Starting a payment transaction with Dimoco failed because we got an incomplete or invalid result from the Dimoco API.");
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = dimocoSettings.FailUrl
                };
            }

            if (actionResponse.ActionResult.Status != 3 && actionResponse.ActionResult.Status != 5)
            {
                logger.LogError($"Starting a payment transaction with Dimoco failed because Dimoco returned the status {actionResponse.ActionResult.Status} and only statuses 3 and 5 are considered a success. Message from Dimoco: {actionResponse.ActionResult.Detail}");
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = dimocoSettings.FailUrl
                };
            }

            foreach (var conceptOrder in conceptOrders)
            {
                conceptOrder.Main.SetDetail(OrderProcessConstants.PaymentProviderTransactionId, actionResponse.Reference);
                conceptOrder.Main.SetDetail(DimocoConstants.DimocoRequestIdProperty, requestId);
                await wiserItemsService.SaveAsync(conceptOrder.Main, skipPermissionsCheck: true);
            }

            return new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = actionResponse.ActionResult.Redirect.Url
            };
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            logger.LogError($"Starting a payment transaction with Dimoco failed because we got an unhandled exception: {exception}");
            return new PaymentRequestResult
            {
                Successful = false,
                Action = PaymentRequestActions.Redirect,
                ActionData = dimocoSettings.FailUrl
            };
        }
        finally
        {
            if (dimocoSettings.LogAllRequests)
            {
                await AddLogEntryAsync(PaymentServiceProviders.Dimoco, invoiceNumber, status, requestFormValues: requestFormValues, responseBody: responseBody, error: error, url: BaseUrl, isIncomingRequest: false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
    {
        var dimocoSettings = (DimocoSettingsModel) paymentMethodSettings.PaymentServiceProvider;
        var error = "";
        var statusCode = 0;
        var responseBody = "";
        try
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                error = "No HTTP context available; unable to process status update.";
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = error,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            // Validate the signature of the webhook.
            var signature = httpContextAccessor.HttpContext.Request.Form[DimocoConstants.WebhookSignatureProperty].ToString();
            using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(dimocoSettings.ClientSecret));
            var hash = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(webHookContents));
            var hashString = Convert.ToHexString(hash);
            if (!String.Equals(hashString, signature, StringComparison.OrdinalIgnoreCase))
            {
                error = "Invalid signature.";
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = error,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            var orderIds = webhookData.CustomParameters.FirstOrDefault(p => p.Key == DimocoConstants.OrderIdsParameterName)?.Value;
            if (String.IsNullOrWhiteSpace(orderIds))
            {
                error = "No order IDs found in webhook data.";
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = error,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            var paymentSuccessful = webhookData.ActionResult.Status == 0;
            if (!paymentSuccessful)
            {
                return new StatusUpdateResult
                {
                    Successful = paymentSuccessful,
                    Status = webhookData.ActionResult.Detail
                };
            }

            // Status 4 and 5 mean successful transactions.
            var successfulTransactions = webhookData.Transactions.Where(t => t.Status is 4 or 5);
            var totalBilled = successfulTransactions.Sum(t => t.BilledAmount);

            // Check if the total billed amount is equal to the total amount of the order, to make sure we don't accept partial payments.
            var orders = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(invoiceNumber);
            var totalAmount = 0M;
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            foreach (var (order, lines) in orders)
            {
                totalAmount += await shoppingBasketsService.GetPriceAsync(order, lines, basketSettings);
            }

            paymentSuccessful = totalBilled >= totalAmount;

            return new StatusUpdateResult
            {
                Successful = paymentSuccessful,
                Status = webhookData.ActionResult.Detail
            };
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            // Log any exceptions that may have occurred.
            logger.LogError(exception, "Error processing Dimoco payment update.");
            return new StatusUpdateResult
            {
                Successful = false,
                Status = "Error processing PayPal payment Dimoco.",
                StatusCode = 500
            };
        }
        finally
        {
            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Dimoco, invoiceNumber, statusCode, error: error);
        }
    }

    /// <inheritdoc />
    public async Task<PaymentServiceProviderSettingsModel> GetProviderSettingsAsync(PaymentServiceProviderSettingsModel paymentServiceProviderSettings)
    {
        databaseConnection.AddParameter("id", paymentServiceProviderSettings.Id);
        var query = $"""
                     SELECT
                         dimocoMerchantIdLive.`value` AS dimocoMerchantIdLive,
                         dimocoMerchantIdTest.`value` AS dimocoMerchantIdTest,
                         dimocoOrderIdLive.`value` AS dimocoOrderIdLive,
                         dimocoOrderIdTest.`value` AS dimocoOrderIdTest,
                         dimocoClientSecretLive.`value` AS dimocoClientSecretLive,
                         dimocoClientSecretTest.`value` AS dimocoClientSecretTest,
                         dimocoMerchantLogoUrlLive.`value` AS dimocoMerchantLogoUrlLive,
                         dimocoMerchantLogoUrlTest.`value` AS dimocoMerchantLogoUrlTest,
                         dimocoServiceNameLive.`value` AS dimocoServiceNameLive,
                         dimocoServiceNameTest.`value` AS dimocoServiceNameTest
                     FROM {WiserTableNames.WiserItem} AS paymentServiceProvider
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoMerchantIdLive ON dimocoMerchantIdLive.item_id = paymentServiceProvider.id AND dimocoMerchantIdLive.`key` = '{DimocoConstants.DimocoMerchantIdLiveProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoMerchantIdTest ON dimocoMerchantIdTest.item_id = paymentServiceProvider.id AND dimocoMerchantIdTest.`key` = '{DimocoConstants.DimocoMerchantIdTestProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoOrderIdLive ON dimocoOrderIdLive.item_id = paymentServiceProvider.id AND dimocoOrderIdLive.`key` = '{DimocoConstants.DimocoOrderIdLiveProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoOrderIdTest ON dimocoOrderIdTest.item_id = paymentServiceProvider.id AND dimocoOrderIdTest.`key` = '{DimocoConstants.DimocoOrderIdTestProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoClientSecretLive ON dimocoClientSecretLive.item_id = paymentServiceProvider.id AND dimocoClientSecretLive.`key` = '{DimocoConstants.DimocoClientSecretLiveProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoClientSecretTest ON dimocoClientSecretTest.item_id = paymentServiceProvider.id AND dimocoClientSecretTest.`key` = '{DimocoConstants.DimocoClientSecretTestProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoMerchantLogoUrlLive ON dimocoMerchantLogoUrlLive.item_id = paymentServiceProvider.id AND dimocoMerchantLogoUrlLive.`key` = '{DimocoConstants.DimocoMerchantLogoUrlLiveProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoMerchantLogoUrlTest ON dimocoMerchantLogoUrlTest.item_id = paymentServiceProvider.id AND dimocoMerchantLogoUrlTest.`key` = '{DimocoConstants.DimocoMerchantLogoUrlTestProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoServiceNameLive ON dimocoServiceNameLive.item_id = paymentServiceProvider.id AND dimocoServiceNameLive.`key` = '{DimocoConstants.DimocoServiceNameLiveProperty}'
                     LEFT JOIN {WiserTableNames.WiserItemDetail} AS dimocoServiceNameTest ON dimocoServiceNameTest.item_id = paymentServiceProvider.id AND dimocoServiceNameTest.`key` = '{DimocoConstants.DimocoServiceNameTestProperty}'
                     WHERE paymentServiceProvider.id = ?id
                     """;

        var result = new DimocoSettingsModel
        {
            Id = paymentServiceProviderSettings.Id,
            Title = paymentServiceProviderSettings.Title,
            Type = paymentServiceProviderSettings.Type,
            LogAllRequests = paymentServiceProviderSettings.LogAllRequests,
            OrdersCanBeSetDirectlyToFinished = paymentServiceProviderSettings.OrdersCanBeSetDirectlyToFinished,
            SkipPaymentWhenOrderAmountEqualsZero = paymentServiceProviderSettings.SkipPaymentWhenOrderAmountEqualsZero
        };
        var dataTable = await databaseConnection.GetAsync(query);
        if (dataTable.Rows.Count == 0)
        {
            return result;
        }
        var row = dataTable.Rows[0];

        var suffix = gclSettings.Environment.InList(Environments.Development, Environments.Test) ? "Test" : "Live";
        result.MerchantId = row.GetAndDecryptSecretKey($"dimocoMerchantId{suffix}");
        result.OrderId = row.GetAndDecryptSecretKey($"dimocoOrderId{suffix}");
        result.ClientSecret = row.GetAndDecryptSecretKey($"dimocoClientSecret{suffix}");
        result.LogoUrl = row.Field<string>($"dimocoMerchantLogoUrl{suffix}")!;
        result.ServiceName = row.Field<string>($"dimocoServiceName{suffix}")!;
        return result;
    }

    /// <inheritdoc />
    public Task<string> GetInvoiceNumberFromRequestAsync()
    {
        try
        {
            if (httpContextAccessor.HttpContext?.Request.Form == null)
            {
                throw new Exception("No HTTP context available.");
            }

            webHookContents = httpContextAccessor.HttpContext.Request.Form[DimocoConstants.WebhookDataProperty].ToString();
            if (String.IsNullOrWhiteSpace(webHookContents))
            {
                throw new Exception("No XML found in body of Dimoco webhook.");
            }

            webhookData = XmlHelpers.DeserializeXml<ApiResultModel>(webHookContents);
            if (webhookData == null)
            {
                throw new Exception("Invalid XML found in body of Dimoco webhook.");
            }

            invoiceNumber = webhookData.CustomParameters.FirstOrDefault(p => p.Key == DimocoConstants.InvoiceNumberParameterName)?.Value;
            if (String.IsNullOrEmpty(invoiceNumber))
            {
                throw new Exception("No invoice number found in body of Dimoco webhook.");
            }

            return Task.FromResult(invoiceNumber);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error getting invoice number from request.");
            throw;
        }
    }
}