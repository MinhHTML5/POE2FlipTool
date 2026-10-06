using POE2FlipTool.Modules;

namespace POE2FlipTool.Charts
{
    /// <summary>
    /// Shows how one item's price moved over a single day, from GGG's hourly currency-exchange digests
    /// (cached by <see cref="ExchangeVolumeService"/>). Three stacked panels share the 00:00 - 23:59 axis:
    /// Exalt (green), Chaos (red), Divine (purple). For every cached hour the highest price is drawn solid,
    /// the lowest dashed, with the range between them shaded.
    /// </summary>
    public class PriceChartForm : Form
    {
        public static readonly Color EXALT_COLOR = Color.FromArgb(0, 150, 0);
        public static readonly Color CHAOS_COLOR = Color.FromArgb(205, 0, 0);
        public static readonly Color DIVINE_COLOR = Color.FromArgb(130, 0, 170);

        private readonly string _itemName;
        private readonly ExchangeVolumeService _volume;
        private readonly ItemNameResolver _names;
        private readonly string? _league;

        private readonly DateTimePicker _datePicker;
        private readonly DayChartControl _chart;
        private readonly Label _lblStatus;

        public PriceChartForm(string itemName, DateTime day, ExchangeVolumeService volume, ItemNameResolver names, string? league)
        {
            _itemName = itemName;
            _volume = volume;
            _names = names;
            _league = league;

            Text = itemName + " - hourly price (GGG exchange data)";
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
                Text = itemName + (league == null ? "" : "  -  " + league),
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
                Text = "One point per hour. Solid = highest price of the hour, dashed = lowest, shaded = range. Hover for details.",
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
            };
            top.Controls.AddRange(new Control[] { lblItem, btnPrev, _datePicker, btnNext, btnRefresh, lblHint });

            // --- chart + status ----------------------------------------------------------------
            _chart = new DayChartControl
            {
                Dock = DockStyle.Fill,
                // points sit at the middle of their hour; describe the hour they summarise
                TimeLabel = t => t.AddMinutes(-30).ToString("HH:mm") + "-" + t.AddMinutes(30).ToString("HH:mm"),
            };
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

        private string _summary = "";

        private void LoadDay()
        {
            DateTime day = _datePicker.Value.Date;
            string? itemId = _names.TryGetId(_itemName);
            List<long> hoursCached = _volume.HoursCachedOn(day);
            List<long> hoursWithLeague = _league == null ? new List<long>() : _volume.HoursWithLeagueOn(_league, day);

            // Work out the one message that explains an empty chart, if any.
            string? problem = null;
            if (_league == null) problem = "No league selected (league list unavailable).";
            else if (itemId == null) problem = "'" + _itemName + "' could not be mapped to a GGG item id, so there is no exchange data for it.";
            else if (hoursCached.Count == 0) problem = "No exchange data cached for " + day.ToString("yyyy-MM-dd") + ". The tool keeps the last " + ExchangeVolumeService.RETENTION_HOURS + " hours and fetches at most " + ExchangeVolumeService.DEFAULT_MAX_BACKFILL_HOURS + " hours per start.";
            else if (hoursWithLeague.Count == 0) problem = "No '" + _league + "' markets in any cached hour of " + day.ToString("yyyy-MM-dd") + ": the league did not exist yet, or has no exchange activity.";

            _chart.Day = day;
            _chart.Panels = new List<ChartPanel>
            {
                BuildPanel("Exalt", EXALT_COLOR, itemId, PoeHttp.EXALTED_ID, day, problem),
                BuildPanel("Chaos", CHAOS_COLOR, itemId, PoeHttp.CHAOS_ID, day, problem),
                BuildPanel("Divine", DIVINE_COLOR, itemId, PoeHttp.DIVINE_ID, day, problem),
            };
            _chart.Invalidate();

            if (problem != null)
            {
                _summary = problem;
            }
            else
            {
                DateTime first = ExchangeVolumeService.HourStartLocal(hoursWithLeague.First());
                DateTime last = ExchangeVolumeService.HourStartLocal(hoursWithLeague.Last()).AddHours(1);
                _summary = hoursWithLeague.Count + " of 24 hours available for " + day.ToString("yyyy-MM-dd")
                    + " (" + first.ToString("HH:mm") + " - " + last.ToString("HH:mm") + ")";
                if (hoursWithLeague.Count < hoursCached.Count)
                {
                    _summary += "; '" + _league + "' has no markets before " + first.ToString("HH:mm") + " (league not started yet)";
                }
            }
            ShowSummary();
        }

        private ChartPanel BuildPanel(string title, Color color, string? itemId, string currencyId, DateTime day, string? problem)
        {
            var high = new List<(DateTime Time, double Value)>();
            var low = new List<(DateTime Time, double Value)>();
            var band = new List<(DateTime Time, double Low, double High)>();

            if (problem == null && itemId != null && _league != null)
            {
                foreach (HourlyPricePoint p in _volume.GetPairHistory(_league, itemId, currencyId, day))
                {
                    DateTime mid = p.HourStartLocal.AddMinutes(30);
                    high.Add((mid, p.MaxPrice));
                    low.Add((mid, p.MinPrice));
                    band.Add((mid, p.MinPrice, p.MaxPrice));
                }
            }

            string unit = title.ToLowerInvariant();
            var panel = new ChartPanel(title, color, new List<ChartSeries>
            {
                new ChartSeries("Highest " + unit + " per item", color, dashed: false, high),
                new ChartSeries("Lowest " + unit + " per item", color, dashed: true, low),
            }, new List<ChartBand> { new ChartBand(color, band) });

            panel.EmptyMessage = problem ?? ("No " + _itemName + " <-> " + title + " trades in the cached hours of " + day.ToString("yyyy-MM-dd"));
            return panel;
        }

        private void ShowSummary()
        {
            _lblStatus.Text = _summary;
        }
    }
}
