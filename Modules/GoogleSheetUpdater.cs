using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POE2FlipTool.Modules
{
    public class GoogleSheetUpdater
    {
        private const string SPREAD_SHEET_ID = "1qakIAj_pZL--0fiQij6rGEKlCVXQxpgQkAJWzaLxNK0";
        private const string SHEET_NAME = "MinhFlip";

        private SheetsService _service;

        public GoogleSheetUpdater()
        {
            GoogleCredential credential;
            using (var stream = new FileStream("google_service.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "C# Google Sheets Update",
            });
        }

        public void UpdateCell (string cell, object value)
        {
            // Define request parameters.
            var range = $"{SHEET_NAME}!{cell}";
            var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>> { new List<object> { value } }
            };
            var updateRequest = _service.Spreadsheets.Values.Update(valueRange, SPREAD_SHEET_ID, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            var updateResponse = updateRequest.Execute();
        }
    }
}
