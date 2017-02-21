using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Net;

namespace Smart365Operations.Common.Infrastructure.Utility
{
    public class RestAPIClient
    {
        #region ------- 单例实现 -------

        private static RestAPIClient _restApiClient;

        // 定义一个标识确保线程同步
        private static readonly object locker = new object();

        // 定义私有构造函数，使外界不能创建该类实例
        private RestAPIClient()
        {
            _client = new RestClient("http://192.168.8.250:8088/365ElectricGuard");

        }

        /// <summary>
        /// 定义公有方法提供一个全局访问点,同时你也可以定义公有属性来提供全局访问点
        /// </summary>
        /// <returns></returns>
        public static RestAPIClient GetInstance()
        {
            // 当第一个线程运行到这里时，此时会对locker对象 "加锁"，
            // 当第二个线程运行该方法时，首先检测到locker对象为"加锁"状态，该线程就会挂起等待第一个线程解锁
            // lock语句运行完之后（即线程运行完之后）会对该对象"解锁"
            // 双重锁定只需要一句判断就可以了
            if (_restApiClient == null)
            {
                lock (locker)
                {
                    // 如果类的实例不存在则创建，否则直接返回
                    if (_restApiClient == null)
                    {
                        _restApiClient = new RestAPIClient();
                    }
                }
            }
            return _restApiClient;
        }

        #endregion

        private RestClient _client;


        public IRestResponse Execute(IRestRequest request)
        {
            return _client.Execute(request);
        }

        public void SetCookie(CookieContainer cookieContainer)
        {
            _client.CookieContainer = cookieContainer;
        }
    }
}
