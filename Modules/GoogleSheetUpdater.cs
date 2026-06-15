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
        private SheetsService _service;
        private string _spreadsheetId;
        private string _sheetName;

        public GoogleSheetUpdater(string spreadsheetId, string sheetName)
        {
            GoogleCredential credential;
            using (var stream = new FileStream("data/google_service.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _spreadsheetId = spreadsheetId;
            _sheetName = sheetName;

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "C# Google Sheets Update",
            });
        }

        public void UpdateCell(string cell, object value)
        {
            // Define request parameters.
            var range = $"{_sheetName}!{cell}";
            var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>> { new List<object> { value } }
            };
            var updateRequest = _service.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.ExecuteAsync();
        }

        public string GetValueFromCell(string cell)
        {
            var range = $"{_sheetName}!{cell}";
            var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            if (values != null && values.Count > 0 && values[0].Count > 0)
            {
                return values[0][0].ToString();
            }
            return null;
        }

        public List<(int, string)> GetValueFromColumn(string column)
        {
            var range = $"{_sheetName}!{column}:{column}";
            var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = request.Execute();

            var result = new List<(int Row, string Value)>();

            if (response.Values == null)
                return result;

            for (int i = 0; i < response.Values.Count; i++)
            {
                var row = response.Values[i];
                string value = row.Count > 0 ? row[0]?.ToString() ?? "" : "";
                result.Add((i + 1, value));
            }

            return result;

        }
    }
}
