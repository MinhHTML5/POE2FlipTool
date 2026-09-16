using POE2FlipTool.DataModel;
using POE2FlipTool.Modules;

namespace POE2FlipTool.Charts
{
    /// <summary>
    /// Shows how one item's prices moved over a single day, read from the history CSVs.
    /// Three stacked panels share the 00:00 - 23:59 axis: Exalt (green), Chaos (red), Divine (purple).
    /// Solid line = what you get when selling the item, dashed = what you pay when buying it.
    /// </summary>
    public class PriceChartForm : Form
    {
        public static readonly Color EXALT_COLOR = Color.FromArgb(0, 150, 0);
        public static readonly Color CHAOS_COLOR = Color.FromArgb(205, 0, 0);
        public static readonly Color DIVINE_COLOR = Color.FromArgb(130, 0, 170);

        private readonly string _itemName;
        private readonly PriceHistoryWriter _history;

        private readonly DateTimePicker _datePicker;
        private readonly DayChartControl _chart;
        private readonly Label _lblStatus;

        public PriceChartForm(string itemName, DateTime day, PriceHistoryWriter history)
        {
            _itemName = itemName;
            _history = history;

            Text = itemName + " - price over the day";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 760);
            MinimumSize = new Size(640, 460);
            ShowIcon = false;

            // --- top bar -----------------------------------------------------------------------
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 6, 8, 0),
                WrapContents = false,
            };
            var lblItem = new Label
            {
                Text = itemName,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 3, 16, 0),
            };
            var btnPrev = new Button { Text = "<", Width = 28, Height = 25, Margin = new Padding(0, 1, 2, 0) };
            _datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = day.Date,
                Width = 120,
                Margin = new Padding(0, 2, 2, 0),
            };
            var btnNext = new Button { Text = ">", Width = 28, Height = 25, Margin = new Padding(0, 1, 12, 0) };
            var btnRefresh = new Button { Text = "Refresh", Width = 70, Height = 25, Margin = new Padding(0, 1, 16, 0) };
            var lblHint = new Label
            {
                Text = "Solid = sell price, dashed = buy price. Hover a point for details.",
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
            };
            top.Controls.AddRange(new Control[] { lblItem, btnPrev, _datePicker, btnNext, btnRefresh, lblHint });

            // --- chart + status ----------------------------------------------------------------
            _chart = new DayChartControl { Dock = DockStyle.Fill };
            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                ForeColor = Color.DimGray,
            };

            // The Fill control must be added first so the docked bars take their space before it.
            Controls.Add(_chart);
            Controls.Add(top);
            Controls.Add(_lblStatus);

            _chart.HoverTextChanged += text =>
            {
                if (text.Length > 0) _lblStatus.Text = text;
                else ShowSummary();
            };
            _datePicker.ValueChanged += (s, e) => LoadDay();
            btnPrev.Click += (s, e) => _datePicker.Value = _datePicker.Value.AddDays(-1);
            btnNext.Click += (s, e) => _datePicker.Value = _datePicker.Value.AddDays(1);
            btnRefresh.Click += (s, e) => LoadDay();

            LoadDay();
        }

        private List<HistoryRecord> _records = new List<HistoryRecord>();

        private void LoadDay()
        {
            DateTime day = _datePicker.Value.Date;
            try
            {
                _records = _history.ReadDay(day)
                    .Where(r => r.Reading.Name == _itemName)
                    .OrderBy(r => r.Reading.Timestamp)
                    .ToList();
            }
            catch (Exception ex)
            {
                _records = new List<HistoryRecord>();
                _lblStatus.Text = "Could not read history: " + ex.Message;
            }

            _chart.Day = day;
            _chart.Panels = new List<ChartPanel>
            {
                BuildPanel("Exalt", EXALT_COLOR, PriceField.SellForEx, PriceField.BuyWithEx),
                BuildPanel("Chaos", CHAOS_COLOR, PriceField.SellForChaos, PriceField.BuyWithChaos),
                BuildPanel("Divine", DIVINE_COLOR, PriceField.SellForDiv, PriceField.BuyWithDiv),
            };
            _chart.Invalidate();
            ShowSummary();
        }

        private ChartPanel BuildPanel(string title, Color color, PriceField sellField, PriceField buyField)
        {
            return new ChartPanel(title, color, new List<ChartSeries>
            {
                new ChartSeries(PriceFields.Header(sellField), color, dashed: false, SeriesPoints(sellField)),
                new ChartSeries(PriceFields.Header(buyField), color, dashed: true, SeriesPoints(buyField)),
            });
        }

        private List<(DateTime Time, double Value)> SeriesPoints(PriceField field)
        {
            return _records
                .Where(r => r.Reading.Get(field).HasValue)
                .Select(r => (r.Reading.Timestamp, r.Reading.Get(field)!.Value))
                .ToList();
        }

        private void ShowSummary()
        {
            DateTime day = _datePicker.Value.Date;
            if (_records.Count == 0)
            {
                _lblStatus.Text = "No readings for " + _itemName + " on " + day.ToString("yyyy-MM-dd") + ".";
            }
            else
            {
                _lblStatus.Text = _records.Count + " readings on " + day.ToString("yyyy-MM-dd")
                    + "  (" + _records.First().Reading.Timestamp.ToString("HH:mm") + " - " + _records.Last().Reading.Timestamp.ToString("HH:mm") + ")";
            }
        }
    }
}
