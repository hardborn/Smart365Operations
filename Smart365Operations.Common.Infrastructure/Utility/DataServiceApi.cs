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
        private static readonly Dictionary<int, string> ResultTable = new Dictionary<int, string>
        {
            {0,"成功" },
            {1,"系统错误" },
            {1001,"未登录" },
            {1002,"用户名或密码错误" },
            {1003,"字段错误" },
            {1004,"没有权限" },
            {1005,"没有此API" },
            {1006,"JSON格式错误" },
            {1007,"未绑定手机" },
            {1008,"已过期" },
            {1009,"验证失败" },
            {1010,"已绑定手机" },
            {1011,"投标已结束" },
            {-1,"" },
        };

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

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var twilioException = new ApplicationException(message, response.ErrorException);
                throw twilioException;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var cookie = response.Cookies.FirstOrDefault();
                if (cookie != null)
                {
                    CookieContainer cookiecon = new CookieContainer();
                    cookiecon.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                    SetCookieContainer(cookiecon);
                }
            }

            T result = default(T);
            var resultObject = JsonConvert.DeserializeObject(response.Content) as JObject;
            if (resultObject != null)
            {
                var responseCode = resultObject.Property("errorCode").Value.Value<int>();
                if (!ResultTable.ContainsKey(responseCode))
                {
                    
                }
                if (responseCode != 0)
                {
                    throw new ApplicationException($"错误：{resultObject.Property("message").Value.Value<string>()}");
                }
                else
                {
                    var responseData = resultObject.Property("data").Value;
                    result = responseData.ToObject<T>();
                }
            }

            return result;
        }

        private void SetCookieContainer(CookieContainer cookieContainer)
        {
            _cookieContainer = cookieContainer;
        }
    }
}
