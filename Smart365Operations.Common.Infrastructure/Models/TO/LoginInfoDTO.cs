using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smart365Operations.Common.Infrastructure.Models.TO
{

    public class LoginInfoDTO
    {
        public string realName { get; set; }
        public Role role { get; set; }
        public string mobile { get; set; }
        public string userType { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
    }

    public class Role
    {
        [JsonIgnore]
        public Permission permission { get; set; }
        public int permissionValue { get; set; }
        public int roleId { get; set; }
        public string roleName { get; set; }
        public string userType { get; set; }
    }

    public class Permission
    {
    }

}
