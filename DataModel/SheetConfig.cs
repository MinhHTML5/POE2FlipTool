using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace POE2FlipTool.DataModel
{
    public class SheetConfig
    {
        public string Type { get; set; } = "A";
        public string Name { get; set; } = "B";
        public string BuyRateDivine { get; set; } = "C";
        public string SellRateDivine { get; set; } = "D";
        public string BuyRateChaos { get; set; } = "E";
        public string SellRateChaos { get; set; } = "F";
        public string BuyRateExalt { get; set; } = "G";
        public string SellRateExalt { get; set; } = "H";
        
        public string ConvertRateChaos { get; set; } = "L3";
        public string ConvertRateExalts { get; set; } = "L4";

        public int StatingRow { get; set; } = 3;

        public static string GetColumnLetter(string cellRef)
        {
            string columnLetter = string.Empty;
            foreach (char c in cellRef)
            {
                if (char.IsLetter(c))
                {
                    columnLetter += c;
                }
                else
                {
                    break;
                }
            }
            return columnLetter;
        }

        public static int GetRowNumber(string cellRef)
        {
            string rowNumberStr = string.Empty;
            foreach (char c in cellRef)
            {
                if (char.IsDigit(c))
                {
                    rowNumberStr += c;
                }
            }
            return int.Parse(rowNumberStr);
        }

    }
}
