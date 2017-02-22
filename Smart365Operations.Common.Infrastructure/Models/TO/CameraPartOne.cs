using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smart365Operations.Common.Infrastructure.Models.TO
{
    
    public class CameraPartOne
    {
        public VideoInfo[] video { get; set; }
        [JsonIgnore]
        public CustomerDTO customer { get; set; }
    }


    public class VideoInfo
    {
        public string videoChannel { get; set; }
        public int videoId { get; set; }
        public string videoName { get; set; }
        public string videoSequence { get; set; }
        public string videoUrl { get; set; }
    }


}
