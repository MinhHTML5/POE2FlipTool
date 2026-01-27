namespace POE2FlipTool.DataModel
{
    public class GeneralConfig
    {
        public string googleSheetID { get; set; }
        public string googleSheetName { get; set; }
        

        public GeneralConfig(string googleSheetID, string googleSheetName)
        {
            this.googleSheetID = googleSheetID;
            this.googleSheetName = googleSheetName;
        }
    }
}
