using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CurrencyConverterFunction
{
    public static class CurrencyConverter
    {
        private static readonly HttpClient client = new HttpClient();

        [Function("CurrencyConverter")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CurrencyConverter");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string baseCurrency = query["base"];
            string targetCurrency = query["target"];
            string amountStr = query["amount"];

            if (string.IsNullOrEmpty(baseCurrency) || string.IsNullOrEmpty(targetCurrency) || string.IsNullOrEmpty(amountStr) || !decimal.TryParse(amountStr, out decimal amount))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Please pass a valid base currency, target currency, and amount on the query string.");
                return badRequestResponse;
            }

            string apiKey = "ffaa0f18889bb1655d8dbc11"; 
            string url = $"https://v6.exchangerate-api.com/v6/{apiKey}/pair/{baseCurrency}/{targetCurrency}/{amount}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic? data = JsonConvert.DeserializeObject(responseBody);

            if (data.result != "success")
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Failed to retrieve exchange rates.");
                return errorResponse;
            }

            decimal convertedAmount = data.conversion_result;

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await okResponse.WriteStringAsync(JsonConvert.SerializeObject(new
            {
                baseCurrency,
                targetCurrency,
                amount,
                convertedAmount
            }));
            return okResponse;
        }
    }
}
