using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.DataModel
{
    public enum ItemCategory
    {
        Currency,
        Essences,
        Delirium,
        Breach,
        Ritual,
        Expedition,
        Abyss,
        Incursion,
        Fragments,
        Runes,
        SoulCores,
        Idols,
        UncutGems,
        Gems,
        Count
    }

    public class TradeItem
    {
        public ItemCategory category { get; set; }
        public string name { get; set; } // Will also be what get typed into regex
        public int itemSelectIndex { get; set; } = 1; // Which of the 3 item select slot to use - Default to middle one
        public string column { get; set; } = "B"; // Which column this item belong to in google sheet

        public TradeItem(ItemCategory category, string name, int itemSelectIndex, string column)
        {
            this.category = category;
            this.name = name;
            this.itemSelectIndex = itemSelectIndex;
            this.column = column;
        }
    }
}
