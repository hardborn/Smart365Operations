using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart365Operation.Modules.VideoMonitoring.Models
{
    public class RegionInfo
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int Index { get; set; }
        public IntPtr DisplayHandler { get; set; }
        public IntPtr SessionId { get; set; } 
        public bool IsDisplaying { get; set; }
    }
}
