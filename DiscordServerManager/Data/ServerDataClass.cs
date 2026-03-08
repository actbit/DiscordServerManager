using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordServerManager.Data
{
    public class ServerDataClass
    {
        public ulong ServerID { get; set; }
        public List<CategoryDataClass> Categorys { get; set; } = new List<CategoryDataClass>();
        public List<ulong> AdminUserIDs { get; set; } = new List<ulong>();
        public List<ulong> AdminRoleIDs{ get; set; }=new List<ulong>();
    }
}
