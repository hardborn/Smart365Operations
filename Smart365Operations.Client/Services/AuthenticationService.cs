using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;
using Smart365Operations.Common.Infrastructure.Models.TO;
using System.Net;
using Smart365Operations.Common.Infrastructure.Utility;

namespace Smart365Operations.Client.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        public User AuthenticateUser(string username, string password)
        {
            
            var request = new RestRequest($"login.json?username={username}&password={password}", Method.GET);
            var response = RestAPIClient.GetInstance().Execute(request);

            CookieContainer cookiecon = new CookieContainer();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var cookie = response.Cookies.FirstOrDefault();
                cookiecon.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }
            RestAPIClient.GetInstance().SetCookie(cookiecon);
            var value =  JsonConvert.DeserializeObject(response.Content) as JObject;

            var s = value.First;
            var responseCode = value.Property("errorCode").Value.Value<int>();
            // var valueItem = value.First.ToObject<Rootobject>();
            return new User(username: username, email: "", roles: new string[] {"admin"});
        }
    }
}
