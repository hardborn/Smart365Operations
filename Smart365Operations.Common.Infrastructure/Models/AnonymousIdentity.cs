using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart365Operations.Common.Infrastructure.Models
{
    public class AnonymousIdentity : SystemIdentity
    {
        public AnonymousIdentity()
            : base(string.Empty,string.Empty, string.Empty, new string[] { })
        { }
    }
}
