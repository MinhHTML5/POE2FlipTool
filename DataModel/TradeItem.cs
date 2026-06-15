using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.DataModel
{
    public class TradeItem
    {
        public string name { get; set; } // Will also be what get typed into regex
        public int itemSelectIndex { get; set; } = 0; // Which of the 3 item select slot to use - Default to the left one
        public int row { get; set; } = 10; // Which row this item belong to in google sheet

        public TradeItem(string name, int row, int itemSelectIndex = 0)
        {
            this.name = name;
            this.row = row;
            this.itemSelectIndex = itemSelectIndex;
        }
    }
}
