using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smart365Operations.Common.Infrastructure.Models
{
    public class VideoInformation
    {
        public VideoInformation()
        {
            
        }
        [JsonProperty("videoChannel")]
        public string VideoChannel { get; set; }
        [JsonProperty("videoId")]
        public int VideoId { get; set; }
        [JsonProperty("videoName")]
        public string VideoName { get; set; }
        [JsonProperty("videoSequence")]
        public string VideoSequence { get; set; }
        [JsonProperty("videoUrl")]
        public string VideoUrl { get; set; }
    }

}
