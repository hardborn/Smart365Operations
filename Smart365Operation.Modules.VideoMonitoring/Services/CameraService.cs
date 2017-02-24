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

            var httpServiceApi = new DataServiceApi();
            var request = new RestRequest($"customer/videolist.json?customerId={customerId}", Method.GET);
            var cameraPartOne = httpServiceApi.Execute<CameraPartOne>(request);

            var cameraPartTwoJson = HkAction.playList();
            var dto = JsonConvert.DeserializeObject<CameraPartTwo>(cameraPartTwoJson);
            if (dto != null)
            {
                foreach (var videoInfo in cameraPartOne.video)
                {
                    var cameraPartTwo = dto.cameras.FirstOrDefault(camera => camera.deviceSerial == videoInfo.videoSequence && camera.cameraNo.ToString() == videoInfo.videoChannel);
                    if (cameraPartTwo != null)
                    {
                        Camera camera = new Camera()
                        {
                            Id = cameraPartTwo.cameraId,
                            Name = videoInfo.videoName,
                            Status = cameraPartTwo.status
                        };
                        cameraList.Add(camera);
                    }
                }
            }
            return cameraList;
        }
    }
}
