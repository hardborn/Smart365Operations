using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart365Operations.Common.Infrastructure.Models.TO
{
   
    public class CustomerDTO
    {
        public long contractTime { get; set; }
        public string customerAddress { get; set; }
        public int customerId { get; set; }
        public string customerIntroduce { get; set; }
        public string customerLinkman { get; set; }
        public string customerName { get; set; }
        public string customerPhone { get; set; }
        public long inTime { get; set; }
        public int installedCapacity { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public int meteringPoint { get; set; }
        public int operatingCapacity { get; set; }
        public int transformerNumber { get; set; }
    }

}
