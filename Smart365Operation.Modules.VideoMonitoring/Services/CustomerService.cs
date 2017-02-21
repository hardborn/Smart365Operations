using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;
using Smart365Operations.Common.Infrastructure.Models.TO;
using Smart365Operations.Common.Infrastructure.Utility;

namespace Smart365Operation.Modules.VideoMonitoring.Services
{
    public class CustomerService : ICustomerService
    {
        public IList<Customer> GetCustomersBy(int agentId)
        {
            List<Customer> customerList = new List<Customer>();
            //var httpClient = new RestClient("http://192.168.8.250:8088/365ElectricGuard");
            var request = new RestRequest($"customer/list.json", Method.GET);
            var response = RestAPIClient.GetInstance().Execute(request);

            var value = JsonConvert.DeserializeObject(response.Content) as JObject;
            var customerDtoList = value.First.First.ToObject<List<CustomerDTO>>();

            foreach (var customerDto in customerDtoList)
            {
                Customer customer = new Customer()
                {
                    Id = customerDto.customerId,
                    Name = customerDto.customerName
                };
                customerList.Add(customer);
            }

            return customerList;
        }
    }
}
