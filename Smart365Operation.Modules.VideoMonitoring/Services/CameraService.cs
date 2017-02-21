using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;
using RestSharp;
using Smart365Operations.Common.Infrastructure.Models.TO;
using Smart365Operation.Modules.VideoMonitoring.Utility;
using Smart365Operations.Common.Infrastructure.Utility;

namespace Smart365Operation.Modules.VideoMonitoring.Services
{
    public class CameraService : ICameraService
    {
        public CameraService()
        {
            if (HkAction.Start())
            {
                if (HkAction.GetAccessToken() != null)
                {

                }
            }
        }
        public IList<Camera> GetCamerasBy(int customerId)
        {
            List<Camera> cameraList = new List<Camera>();

            // var httpClient = new RestClient("http://192.168.8.250:8088/365ElectricGuard");
            var request = new RestRequest($"customer/videolist.json?customerId={customerId}", Method.GET);
            var response = RestAPIClient.GetInstance().Execute(request);

            var value = JsonConvert.DeserializeObject(response.Content) as JObject;
            var cameraPartOneList = value.First.First.First.First.ToObject<List<CameraPartOne>>();



            var cameraPartTwoJson = HkAction.playList();
            var dto = JsonConvert.DeserializeObject<CameraPartTwo>(cameraPartTwoJson);
            foreach (var cameraPartOne in cameraPartOneList)
            {
                var cameraPartTwo = dto.cameras.FirstOrDefault(camera => camera.deviceSerial == cameraPartOne.videoSequence && camera.cameraNo.ToString() == cameraPartOne.videoChannel);
                if (cameraPartTwo != null)
                {
                    Camera camera = new Camera()
                    {
                        Id = cameraPartTwo.cameraId,
                        Name = cameraPartOne.videoName,
                        Status = cameraPartTwo.status
                    };
                    cameraList.Add(camera);
                }
            }



            return cameraList;
        }
    }
}
