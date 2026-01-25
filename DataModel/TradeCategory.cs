using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.DataModel
{
    public class TradeCategory
    {
        public string name { get; set; } // Will also be what get typed into regex
        public float x { get; set; }
        public float y { get; set; }

        public TradeCategory(string name, float x, float y)
        {
            this.name = name;
            this.x = x;
            this.y = y;
        }
    }
}
