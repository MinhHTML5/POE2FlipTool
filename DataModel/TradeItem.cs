using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.DataModel
{
    public class TradeItem
    {
        private static string[] weirdIndexList = new string[] { "Augmentation", "Transmutation", "Regal", "Exalted", "Chaos", "Jeweller's", "Essence" };

        public string category { get; set; }
        public string name { get; set; }
        public int itemSelectIndex
        {
            get
            {
                if (!name.Contains("Greater") && !name.Contains("Perfect") && weirdIndexList.Contains(name))
                {
                    return 0;
                }
                return 1;
            }
        }

        public TradeItem(string category, string name)
        {
            this.category = category;
            this.name = name;
        }
    }
}
