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
            DataServiceApi httpServiceApi = new DataServiceApi();
            var request = new RestRequest($"login.json?username={username}&password={password}", Method.GET);
            var loginInfo = httpServiceApi.Execute<LoginInfoDTO>(request);

            return new User(id:loginInfo.userId.ToString(), username: username, email: "", roles: new string[] { loginInfo.userType });
        }
    }
}
