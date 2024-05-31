using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentApp.Models;
using RestSharp;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace PaymentApp.Services
{
    public class PaymentService() : IPayment
    {
        private readonly RestClient _client = new("https://secure.paygate.co.za/payhost/process.trans");

        public async Task<CardPaymentResponse> AddNewCard(Card card)
        {
            var payResponse = new CardPaymentResponse
            {
                Completed = false
            };

            RestRequest request = new(Method.Post.ToString());
            request.AddHeader("Content-Type", "text/xml");
            request.AddHeader("SOAPAction", "WebPaymentRequest");

            // request body
            string body;
            using (StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "/Templates/SinglePaymentRequest.xml"))
            {
                body = await reader.ReadToEndAsync();
            }

            body = body.Replace("{PayGateId}", "");
            body = body.Replace("{Password}", "");
            body = body.Replace("{FirstName}", card.FirstName);
            body = body.Replace("{LastName}", card.LastName);
            body = body.Replace("{Mobile}", "");
            body = body.Replace("{Email}", card.Email);
            body = body.Replace("{CardNumber}", card.CardNumber.ToString());
            body = body.Replace("{CardExpiryDate}", card.CardExpiry.ToString());
            body = body.Replace("{CVV}", card.Cvv.ToString());
            // body = body.Replace("{Vault}", false.ToString());
            body = body.Replace("{MerchantOrderId}", Guid.NewGuid().ToString());
            // convert amount to cents (amount * 100)
            body = body.Replace("{Amount}", (card.Amount * 100).ToString("0000"));
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            var response = await _client.ExecuteAsync(request);

            string[] map = { "SinglePaymentResponse", "CardPaymentResponse" };
            JToken? result = MapXmlResponseToObject(response.Content, map);
            // check payment response
            if (result?["Status"] != null)
            {
                payResponse.Response = JsonConvert.SerializeObject(result);
                JToken? paymentStatus = result["Status"];
                switch (paymentStatus?["StatusName"]?.ToString())
                {
                    case "Error":
                        throw new ApplicationException();

                    case "Completed":
                        if ((bool)paymentStatus && paymentStatus?["ResultCode"] != null)
                        {
                            payResponse.Completed = true;
                            payResponse.PayRequestId = paymentStatus?["PayRequestId"]?.ToString();
                            payResponse.Secure3DHtml = null;

                            if (paymentStatus?["ResultCode"]?.ToString() == "990017")
                            {
                                return payResponse;
                            }

                            throw new ApplicationException($"{paymentStatus?["ResultCode"]}: Payment declined");
                        }
                        break;

                    case "ThreeDSecureRedirectRequired":
                        // payment requires 3D verification
                        JToken? redirectXml = result["Redirect"];
                        if (redirectXml?["UrlParams"] != null)
                        {
                            RestClient client = new RestClient(redirectXml["RedirectUrl"]?.ToString()!);
                            JArray urlParams = JArray.Parse(redirectXml["UrlParams"]?.ToString()!);
                            Dictionary<string, string> urlParamsDictionary = urlParams.Cast<JObject>()
                                .ToDictionary(item => item.GetValue("key")?.ToString(),
                                    item => item.GetValue("value")?.ToString())!;
                            string httpRequest = ToUrlEncodedString(urlParamsDictionary!);

                            RestRequest req = new(Method.Post.ToString());
                            req.AddParameter("application/x-www-form-urlencoded", httpRequest,
                                ParameterType.RequestBody);
                            var res = await client.ExecuteAsync(req);

                            if (!res.IsSuccessful) throw new ApplicationException(res.ErrorMessage);

                            string responseContent = res.Content;
                            payResponse.Completed = false;
                            payResponse.Secure3DHtml = responseContent;
                            payResponse.PayRequestId = urlParamsDictionary["PAY_REQUEST_ID"];
                            return payResponse;
                        }
                        break;
                }
            }
            throw new ApplicationException("Payment request returned no results");
        }

        public async Task<JToken?> GetVaultedCard(string vaultId)
        {
            RestRequest request = new RestRequest(Method.Post.ToString());
            request.AddHeader("Content-Type", "text/xml");
            request.AddHeader("SOAPAction", "SingleVaultRequest");

            // request body
            string body;
            using (StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "/Templates/SingleVaultRequest.xml"))
            {
                body = await reader.ReadToEndAsync();
            }

            body = body.Replace("{PayGateId}", "");
            body = body.Replace("{Password}", "");
            body = body.Replace("{VaultId}", vaultId);
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            var response = await _client.ExecuteAsync(request);

            string[] map = ["SingleVaultResponse", "LookUpVaultResponse"];
            return MapXmlResponseToObject(response.Content, map);
        }

        public async Task<JToken?> QueryTransaction(string payRequestId)
        {
            RestRequest request = new RestRequest(Method.Post.ToString());
            request.AddHeader("Content-Type", "text/xml");
            request.AddHeader("SOAPAction", "SingleFollowUpRequest");

            // request body
            string body;
            using (StreamReader reader = new StreamReader(Directory.GetCurrentDirectory() + "/Templates/SingleFollowUpRequest.xml"))
            {
                body = await reader.ReadToEndAsync();
            }

            body = body.Replace("{PayGateId}", "");
            body = body.Replace("{Password}", "");
            body = body.Replace("{PayRequestId}", payRequestId);
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            var response = await _client.ExecuteAsync(request);

            string[] map = ["SingleFollowUpResponse", "QueryResponse"];
            return MapXmlResponseToObject(response.Content, map);
        }

        private static JToken? MapXmlResponseToObject(string xmlContent, string[]? responseKeys)
        {
            XmlDocument xmlResult = new XmlDocument();
            // throws exception if it fails to parse xml
            xmlResult.LoadXml(xmlContent);
            // convert to json
            string result = JsonConvert.SerializeXmlNode(xmlResult);
            // remove prefix tags
            result = Regex.Replace(result, @"\bns2:\b", "");
            // parse as json object
            JObject paymentResponse = JObject.Parse(result);
            // return response
            JToken? response = paymentResponse["SOAP-ENV:Envelope"]?["SOAP-ENV:Body"];
            if (responseKeys != null)
            {
                response = responseKeys.Aggregate(response, (current, t) => current?[t]);
            }
            return response;
        }

        private static string ToUrlEncodedString(Dictionary<string, string?> request)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string key in request.Keys)
            {
                builder.Append("&");
                builder.Append(key);
                builder.Append("=");
                builder.Append(HttpUtility.UrlEncode(request[key]));
            }
            string result = builder.ToString().TrimStart('&');
            return result;
        }
    }
}