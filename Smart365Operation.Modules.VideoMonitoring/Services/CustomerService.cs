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
            var httpServiceApi = new DataServiceApi();
            var request = new RestRequest($"customer/list.json", Method.GET);
            var customerDtoList = httpServiceApi.Execute<List<CustomerDTO>>(request);


            IList<Customer> customerList = new List<Customer>();
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
