using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Smart365Operations.Common.Infrastructure.Utility
{
    public class DataServiceApi
    {
        private const string BaseUrl = "http://192.168.8.250:8088/365ElectricGuard";

        private static CookieContainer _cookieContainer = new CookieContainer();

        public DataServiceApi()
        {

        }



        public T Execute<T>(RestRequest request) where T : new()
        {
            var client = new RestClient
            {
                BaseUrl = new System.Uri(BaseUrl),
                CookieContainer = _cookieContainer
            };
            var response = client.Execute(request);

            CookieContainer cookiecon = new CookieContainer();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var cookie = response.Cookies.FirstOrDefault();
                cookiecon.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }

            client.CookieContainer = cookiecon;

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var twilioException = new ApplicationException(message, response.ErrorException);
                throw twilioException;
            }

            T result = default(T);
            var resultObject = JsonConvert.DeserializeObject(response.Content) as JObject;
            if (resultObject != null)
            {
                var responseCode = resultObject.Property("errorCode").Value.Value<int>();
                if (responseCode != 0)
                {
                    throw new ApplicationException($"错误：{resultObject.Property("message").Value.Value<string>()}");
                }
                else
                {
                    var responseData = resultObject.Property("errorCode").Value;
                    result = responseData.ToObject<T>();
                }
            }

            return result;
        }
    }
}
