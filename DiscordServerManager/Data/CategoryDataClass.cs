using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordServerManager.Data
{
    public class CategoryDataClass
    {
        public ulong CategoryID { get; set; }
        public ulong? RoleId { get; set; } = null;
        public ulong? UserId { get; set; } = null;

        //public MakeChannelBaseClass ChannelBase { get; set; } = new MakeChannelBaseClass();
    }
}
