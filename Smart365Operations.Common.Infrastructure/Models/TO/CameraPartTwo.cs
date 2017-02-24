using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smart365Operations.Common.Infrastructure.Models.TO
{
    public class CameraPartTwo
    {
        [JsonProperty("cameraList")]
        public List<CameraInfoDTO> cameras { get; set; } = new List<CameraInfoDTO>();
        public int count { get; set; }
        public string resultCode { get; set; }
    }

    public class CameraInfoDTO
    {
        public string cameraId { get; set; }
        public string cameraName { get; set; }
        public int cameraNo { get; set; }
        public int defence { get; set; }
        public string deviceId { get; set; }
        public string deviceName { get; set; }
        public string deviceSerial { get; set; }
        public int isEncrypt { get; set; }
        public string isShared { get; set; }
        public string picUrl { get; set; }
        public int status { get; set; }
        public int videoLevel { get; set; }
    }
}
