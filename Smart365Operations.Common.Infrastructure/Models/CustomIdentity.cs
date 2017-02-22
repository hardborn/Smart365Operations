using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Smart365Operations.Common.Infrastructure.Models
{
    public class SystemIdentity : IIdentity
    {
        public SystemIdentity(string id,string name, string email, string[] roles)
        {
            Id = id;
            Name = name;
            Email = email;
            Roles = roles;
        }

        public string Id { get; set; }

        public string Email { get; private set; }
        public string[] Roles { get; private set; }

        public string AuthenticationType
        {
            get { return "Custom authentication"; }
        }

        public bool IsAuthenticated
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        public string Name { get; private set; }
    }
}
