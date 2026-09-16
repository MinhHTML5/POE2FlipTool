using POE2FlipTool.Charts;
using POE2FlipTool.DataModel;
using POE2FlipTool.Modules;
using POE2FlipTool.Utilities;
using System.Diagnostics;
using System.Globalization;
using Timer = System.Windows.Forms.Timer;


namespace POE2FlipTool
{
    public partial class Main : Form
    {
        public static Main sInstance;

        public const int UPDATE_INTERVAL = 10;

        private WindowsUtil _windowsUtil;
        private InputHook _inputHook;
        private ColorUtil _colorUtil;
        private OCRUtil _ocrUtil;

        private GeneralConfig _generalConfig;
        private GoogleSheetUpdater _googleSheetUpdater;
        private PricingChecker _pricingChecker;
        private PriceHistoryWriter _history;
        private readonly PriceBoard _board = new PriceBoard();
        private readonly LeagueService _leagueService = new LeagueService();
        private ExchangeVolumeService _volume;
        private ItemNameResolver _itemNames;

        /// <summary>League chosen in the dropdown, or null while the list is unavailable.</summary>
        public string? SelectedLeague => cmbLeague.Enabled ? cmbLeague.SelectedItem as string : null;

        private bool _started = false;
        private Timer _timer = new Timer();
        private Stopwatch _stopwatch = new Stopwatch();

        public Main(WindowsUtil windowsUtil, InputHook inputHook, ColorUtil colorUtil, OCRUtil ocrUtil)
        {
            sInstance = this;

            _windowsUtil = windowsUtil;
            _inputHook = inputHook;
            _colorUtil = colorUtil;
            _ocrUtil = ocrUtil;

            InitializeComponent();
        }

        private const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                _inputHook.ProcessRawInput(m.LParam);
            }

            base.WndProc(ref m);
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            _inputHook.RegisterRawInputDevices(this.Handle, OnMouseKeyEvent, OnKeyEvent);

            _timer.Interval = UPDATE_INTERVAL;
            _timer.Tick += (s, e) => MainLoop();
            _timer.Start();
            _stopwatch.Start();

            DialogResult result = MessageBox.Show(
                "Load POE2 config? (Select no and it'll choose POE1)",
                "POE2?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (result == DialogResult.Yes)
            {
                ConfigReader.poeConfig = "poe2";
                lblPOEChoosed.Text = "POE2";
            }
            else
            {
                ConfigReader.poeConfig = "poe1";
                lblPOEChoosed.Text = "POE1";
            }

            _generalConfig = ConfigReader.ReadGeneralConfig();

            _googleSheetUpdater = new GoogleSheetUpdater(_generalConfig.googleSheetID, _generalConfig.googleSheetName);
            _history = new PriceHistoryWriter(ConfigReader.poeConfig);
            _volume = new ExchangeVolumeService(ConfigReader.poeConfig);
            _itemNames = new ItemNameResolver(ConfigReader.poeConfig);
            _pricingChecker = new PricingChecker(this, _windowsUtil, _inputHook, _colorUtil, _ocrUtil, _googleSheetUpdater, _board, _history);
            _pricingChecker.Init();

            InitPriceGrid();
            LoadBoardFromSheet();
            LoadBoardFromHistory();
            LoadCategories();
            RefreshRateBoxes();
            RefreshPriceGrid();

            await LoadLeaguesAsync();
            await RefreshMarketDataAsync();
        }

        // ------------------------------------------------------------------
        // GGG trade volume + item name translation
        // ------------------------------------------------------------------

        private bool _marketRefreshRunning = false;

        /// <summary>
        /// Called at startup and whenever a scan starts. Pulls the hourly exchange digest if the last call was an
        /// hour or more ago, translates any metadata ids we have not seen before, and redraws the grid if it is
        /// showing volume. Never throws; failures just leave the previous cached data in place.
        /// </summary>
        private async Task RefreshMarketDataAsync()
        {
            if (_marketRefreshRunning) return;
            _marketRefreshRunning = true;
            try
            {
                await _volume.RefreshIfDueAsync();
                await _itemNames.ResolveMissingAsync(_volume.AllItemIds(), SelectedLeague);
            }
            catch (Exception)
            {
                // keep whatever is cached
            }
            finally
            {
                _marketRefreshRunning = false;
            }

            if (ShowTradeVolume)
            {
                ApplyPriceColumnMode();
                RefreshPriceGrid();
            }
        }

        private bool ShowTradeVolume => chkShowTradeVolume.Checked;

        private void chkShowTradeVolume_CheckedChanged(object sender, EventArgs e)
        {
            ApplyPriceColumnMode();
            RefreshPriceGrid();
        }

        private void cmbLeague_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ShowTradeVolume)
            {
                RefreshPriceGrid();
            }
        }

        /// <summary>Switches the six price columns between editable prices and read-only hourly trade volumes.</summary>
        private void ApplyPriceColumnMode()
        {
            string hourText = _volume.DataHourLocal.HasValue
                ? _volume.DataHourLocal.Value.ToString("HH:mm") + "-" + _volume.DataHourLocal.Value.AddHours(1).ToString("HH:mm")
                : "no data";

            foreach (DataGridViewColumn column in dgvPrices.Columns)
            {
                if (column.Tag is not PriceField field) continue;

                if (ShowTradeVolume)
                {
                    string currency = CurrencyLabel(field);
                    column.HeaderText = IsSellColumn(field)
                        ? "Items traded\nfor " + currency + "\n" + hourText
                        : currency + " traded\nfor item\n" + hourText;
                    column.ReadOnly = true;
                    column.DefaultCellStyle.Format = "#,0";
                }
                else
                {
                    column.HeaderText = PriceFields.Header(field) + "\n(" + PriceFields.SheetColumn(field) + ")";
                    column.ReadOnly = false;
                    column.DefaultCellStyle.Format = PRICE_FORMAT;
                }
            }
        }

        private static bool IsSellColumn(PriceField field)
        {
            return field == PriceField.SellForDiv || field == PriceField.SellForEx || field == PriceField.SellForChaos;
        }

        private static string CurrencyLabel(PriceField field)
        {
            switch (field)
            {
                case PriceField.SellForDiv:
                case PriceField.BuyWithDiv: return "div";
                case PriceField.SellForEx:
                case PriceField.BuyWithEx: return "ex";
                default: return "chaos";
            }
        }

        private static string CurrencyIdFor(PriceField field)
        {
            switch (field)
            {
                case PriceField.SellForDiv:
                case PriceField.BuyWithDiv: return PoeHttp.DIVINE_ID;
                case PriceField.SellForEx:
                case PriceField.BuyWithEx: return PoeHttp.EXALTED_ID;
                default: return PoeHttp.CHAOS_ID;
            }
        }

        /// <summary>
        /// Hourly volume of the item's market against the column's currency in the selected league.
        /// "Sell for X" columns show how many items were traded, "Buy with X" columns how much X was traded.
        /// Null when the item id is unknown, no league is selected, or the pair had no trades.
        /// </summary>
        private double? VolumeFor(ItemReading item, PriceField field)
        {
            string? league = SelectedLeague;
            if (league == null) return null;

            string? itemId = _itemNames.TryGetId(item.Name);
            if (itemId == null) return null;

            var volume = _volume.GetPairVolume(league, itemId, CurrencyIdFor(field));
            if (volume == null) return null;

            return IsSellColumn(field) ? volume.Value.Items : volume.Value.Currency;
        }

        // ------------------------------------------------------------------
        // Leagues
        // ------------------------------------------------------------------

        /// <summary>Fills the league dropdown from GGG's public league list; the first entry (current challenge league) is selected.</summary>
        private async Task LoadLeaguesAsync()
        {
            cmbLeague.Enabled = false;
            cmbLeague.Items.Clear();
            cmbLeague.Items.Add("Loading leagues...");
            cmbLeague.SelectedIndex = 0;

            List<string> leagues;
            try
            {
                leagues = await _leagueService.GetLeaguesAsync(ConfigReader.poeConfig);
            }
            catch (Exception)
            {
                leagues = new List<string>();
            }

            cmbLeague.Items.Clear();
            if (leagues.Count == 0)
            {
                cmbLeague.Items.Add("(league list unavailable)");
                cmbLeague.SelectedIndex = 0;
                cmbLeague.Enabled = false;
                return;
            }

            foreach (string league in leagues)
            {
                cmbLeague.Items.Add(league);
            }
            cmbLeague.SelectedIndex = 0;
            cmbLeague.Enabled = true;
        }

        // ------------------------------------------------------------------
        // Div -> Ex / Div -> Chaos rate boxes
        // ------------------------------------------------------------------

        private bool _refreshingRates = false;

        /// <summary>Shows the board's current rates in the two text boxes without triggering their edit handlers.</summary>
        private void RefreshRateBoxes()
        {
            _refreshingRates = true;
            txtDivToEx.Text = FormatRate(_board.Rates.DivToEx);
            txtDivToChaos.Text = FormatRate(_board.Rates.DivToChaos);
            _refreshingRates = false;
        }

        private static string FormatRate(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : "";
        }

        /// <summary>Called whenever a rate changes (scan reading or manual edit): recompute every profit and redraw.</summary>
        public void OnRatesChanged()
        {
            _board.RecalculateAll();
            RefreshRateBoxes();
            RefreshPriceGrid();
        }

        private void txtDivToEx_Validated(object sender, EventArgs e)
        {
            CommitRate(txtDivToEx, _board.Rates.DivToEx, v => _board.Rates.DivToEx = v, PriceBoard.DIV_TO_EX_CELL);
        }

        private void txtDivToChaos_Validated(object sender, EventArgs e)
        {
            CommitRate(txtDivToChaos, _board.Rates.DivToChaos, v => _board.Rates.DivToChaos = v, PriceBoard.DIV_TO_CHAOS_CELL);
        }

        /// <summary>Enter commits the box immediately instead of waiting for focus to leave.</summary>
        private void txtRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (ReferenceEquals(sender, txtDivToEx)) txtDivToEx_Validated(sender, EventArgs.Empty);
            else if (ReferenceEquals(sender, txtDivToChaos)) txtDivToChaos_Validated(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Applies a manually typed rate. A non-number or a value of zero or less is rejected and the old value
        /// restored. A valid change recomputes all profits and is mirrored to the sheet's CONFIG cell.
        /// </summary>
        private void CommitRate(TextBox box, double? current, Action<double?> set, string sheetCell)
        {
            if (_refreshingRates) return;

            if (!TryParseCell(box.Text, out double? value) || (value.HasValue && value.Value <= 0))
            {
                System.Media.SystemSounds.Beep.Play();
                RefreshRateBoxes();
                return;
            }

            if (value == current)
            {
                RefreshRateBoxes();
                return;
            }

            set(value);
            OnRatesChanged();

            if (!value.HasValue) return;
            try
            {
                _googleSheetUpdater.UpdateCell(sheetCell, value.Value.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rate updated locally, but writing it to the sheet failed:" + Environment.NewLine + ex.Message,
                    "Save rate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ------------------------------------------------------------------
        // Board loading
        // ------------------------------------------------------------------

        /// <summary>Pulls the item list, gold costs and CONFIG rates from columns A:B of the sheet.</summary>
        private bool LoadBoardFromSheet()
        {
            try
            {
                _board.LoadFromSheet(_googleSheetUpdater.GetRows("A1:B"));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load items from Google Sheet:" + Environment.NewLine + ex.Message,
                    "Google Sheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>
        /// Fills the board with the last known price of every item from the history CSVs. When the newest
        /// record of an item is incomplete, older records fill the gaps.
        /// </summary>
        private void LoadBoardFromHistory()
        {
            try
            {
                _board.LoadFromHistory(_history.ReadAll());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read price history from " + _history.Directory + ":" + Environment.NewLine + ex.Message,
                    "Price history", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ------------------------------------------------------------------
        // Categories
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates one checkbox per category header found in the sheet
        /// (a cell containing "~", e.g. "!! ~ FRAGMENT ~ !!"). All start checked.
        /// </summary>
        private void LoadCategories()
        {
            var previouslyUnchecked = flpCategories.Controls.OfType<CheckBox>()
                .Where(c => !c.Checked).Select(c => c.Text).ToHashSet();

            flpCategories.SuspendLayout();
            flpCategories.Controls.Clear();

            foreach (string name in _board.Categories)
            {
                flpCategories.Controls.Add(new CheckBox
                {
                    Text = name,
                    Checked = !previouslyUnchecked.Contains(name),
                    AutoSize = true,
                    Margin = new Padding(6, 3, 3, 3)
                });
            }

            flpCategories.ResumeLayout();
        }

        /// <summary>
        /// Names of the categories whose checkbox is currently ticked.
        /// </summary>
        public HashSet<string> GetEnabledCategories()
        {
            return flpCategories.Controls.OfType<CheckBox>()
                .Where(c => c.Checked)
                .Select(c => c.Text)
                .ToHashSet();
        }

        private void SetAllCategories(bool checkedState)
        {
            foreach (var chk in flpCategories.Controls.OfType<CheckBox>())
            {
                chk.Checked = checkedState;
            }
        }

        private void btnCategoriesAll_Click(object sender, EventArgs e)
        {
            SetAllCategories(true);
        }

        private void btnCategoriesNone_Click(object sender, EventArgs e)
        {
            SetAllCategories(false);
        }

        private void btnCategoriesReload_Click(object sender, EventArgs e)
        {
            if (LoadBoardFromSheet())
            {
                LoadBoardFromHistory();
                LoadCategories();
                RefreshRateBoxes();
                RefreshPriceGrid();
            }
        }

        // ------------------------------------------------------------------
        // Price grid (sheet columns D..N)
        // ------------------------------------------------------------------

        private const string COL_CATEGORY = "colCategory";
        private const string COL_ITEM = "colItem";
        private const string COL_LAST_READ = "colLastRead";
        private const string PRICE_FORMAT = "0.####";
        private const string PROFIT_PER_1M_FORMAT = "0.00";
        private const string PROFIT_PER_DIV_FORMAT = "0.####";

        /// <summary>
        /// A read-only profit column. It shows either divine profit per 1M gold (sheet F/H/L/N) or divine
        /// profit per divine invested (sheet T/X/AB/AF), depending on the "Show profit per div" toggle.
        /// </summary>
        private sealed class ProfitColumnSpec
        {
            public string Name { get; }
            public string Route { get; }
            public Func<ItemReading, double?> Per1M { get; }
            public Func<ItemReading, double?> PerDiv { get; }

            public ProfitColumnSpec(string name, string route, Func<ItemReading, double?> per1M, Func<ItemReading, double?> perDiv)
            {
                Name = name;
                Route = route;
                Per1M = per1M;
                PerDiv = perDiv;
            }

            public string Header(bool perDiv) => (perDiv ? "Profit/div\n" : "Profit/1M\n") + Route;
            public double? Value(ItemReading item, bool perDiv) => perDiv ? PerDiv(item) : Per1M(item);
        }

        private static readonly ProfitColumnSpec[] PROFIT_COLUMNS =
        {
            new ProfitColumnSpec("colProfitExDiv", "buy ex, sell div", r => r.ProfitBuyExSellDiv, r => r.ProfitPerDivBuyExSellDiv),
            new ProfitColumnSpec("colProfitChaosDiv", "buy chaos, sell div", r => r.ProfitBuyChaosSellDiv, r => r.ProfitPerDivBuyChaosSellDiv),
            new ProfitColumnSpec("colProfitDivEx", "buy div, sell ex", r => r.ProfitBuyDivSellEx, r => r.ProfitPerDivBuyDivSellEx),
            new ProfitColumnSpec("colProfitDivChaos", "buy div, sell chaos", r => r.ProfitBuyDivSellChaos, r => r.ProfitPerDivBuyDivSellChaos),
        };

        private bool _refreshingGrid = false;
        private bool ShowProfitPerDiv => chkShowProfitPerDiv.Checked;

        // Sorting is done by rebuilding the rows from the board, so the grid can be put back in sheet order.
        private ProfitColumnSpec? _sortColumn = null;
        private bool _sortDescending = true;

        private void InitPriceGrid()
        {
            dgvPrices.AutoGenerateColumns = false;
            dgvPrices.Columns.Clear();

            AddTextColumn(COL_CATEGORY, "Category", 90);
            AddTextColumn(COL_ITEM, "Item", 170);

            // Same order as the sheet: D E F G H | J K L M N
            AddPriceColumn(PriceField.SellForDiv);
            AddPriceColumn(PriceField.BuyWithEx);
            AddProfitColumn(PROFIT_COLUMNS[0]);
            AddPriceColumn(PriceField.BuyWithChaos);
            AddProfitColumn(PROFIT_COLUMNS[1]);
            AddPriceColumn(PriceField.BuyWithDiv);
            AddPriceColumn(PriceField.SellForEx);
            AddProfitColumn(PROFIT_COLUMNS[2]);
            AddPriceColumn(PriceField.SellForChaos);
            AddProfitColumn(PROFIT_COLUMNS[3]);

            AddTextColumn(COL_LAST_READ, "Last read", 125);

            dgvPrices.CellValueChanged += dgvPrices_CellValueChanged;
            dgvPrices.CellFormatting += dgvPrices_CellFormatting;
            dgvPrices.CellDoubleClick += dgvPrices_CellDoubleClick;
            dgvPrices.ColumnHeaderMouseClick += dgvPrices_ColumnHeaderMouseClick;
            dgvPrices.DataError += (s, e) => e.ThrowException = false;
        }

        // ------------------------------------------------------------------
        // Sorting by a profit column
        // ------------------------------------------------------------------

        /// <summary>First click sorts best profit first, the next click flips the direction.</summary>
        private void dgvPrices_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || dgvPrices.Columns[e.ColumnIndex].Tag is not ProfitColumnSpec spec) return;

            if (ReferenceEquals(_sortColumn, spec))
            {
                _sortDescending = !_sortDescending;
            }
            else
            {
                _sortColumn = spec;
                _sortDescending = true;
            }
            RefreshPriceGrid();
        }

        private void btnResetSort_Click(object sender, EventArgs e)
        {
            _sortColumn = null;
            _sortDescending = true;
            RefreshPriceGrid();
        }

        /// <summary>Board items in the order the grid should show them: sheet order, or by the chosen profit column with unknown values last.</summary>
        private IEnumerable<ItemReading> ItemsInGridOrder()
        {
            if (_sortColumn == null)
            {
                return _board.Items;
            }

            ProfitColumnSpec spec = _sortColumn;
            bool perDiv = ShowProfitPerDiv;
            var known = _board.Items.Where(i => spec.Value(i, perDiv).HasValue);
            var unknown = _board.Items.Where(i => !spec.Value(i, perDiv).HasValue);
            var sorted = _sortDescending
                ? known.OrderByDescending(i => spec.Value(i, perDiv)!.Value)
                : known.OrderBy(i => spec.Value(i, perDiv)!.Value);
            return sorted.Concat(unknown);
        }

        private void UpdateSortGlyphs()
        {
            foreach (DataGridViewColumn column in dgvPrices.Columns)
            {
                bool active = _sortColumn != null && ReferenceEquals(column.Tag, _sortColumn);
                column.HeaderCell.SortGlyphDirection = !active ? SortOrder.None
                    : _sortDescending ? SortOrder.Descending : SortOrder.Ascending;
            }
        }

        // ------------------------------------------------------------------
        // Profit per 1M gold  <->  profit per div
        // ------------------------------------------------------------------

        private void chkShowProfitPerDiv_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn column in dgvPrices.Columns)
            {
                if (column.Tag is ProfitColumnSpec spec)
                {
                    column.HeaderText = spec.Header(ShowProfitPerDiv);
                    column.DefaultCellStyle.Format = ShowProfitPerDiv ? PROFIT_PER_DIV_FORMAT : PROFIT_PER_1M_FORMAT;
                }
            }
            RefreshPriceGrid();
        }

        /// <summary>Double-clicking an item name opens that item's price chart for today.</summary>
        private void dgvPrices_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvPrices.Columns[e.ColumnIndex].Name != COL_ITEM) return;
            if (dgvPrices.Rows[e.RowIndex].Tag is not ItemReading item) return;

            var chart = new PriceChartForm(item.Name, DateTime.Today, _history);
            chart.Show(this);
        }

        private void AddTextColumn(string name, string header, int width)
        {
            dgvPrices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { BackColor = SystemColors.Control }
            });
        }

        /// <summary>Editable price column. Tag holds the PriceField so edits know which value they change.</summary>
        private void AddPriceColumn(PriceField field)
        {
            dgvPrices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "col" + field,
                HeaderText = PriceFields.Header(field) + "\n(" + PriceFields.SheetColumn(field) + ")",
                Tag = field,
                Width = 80,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Format = PRICE_FORMAT, Alignment = DataGridViewContentAlignment.MiddleRight }
            });
        }

        /// <summary>Read-only, sortable by clicking the header. Tag holds the spec so both display modes and sorting know the column.</summary>
        private void AddProfitColumn(ProfitColumnSpec spec)
        {
            dgvPrices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = spec.Name,
                HeaderText = spec.Header(ShowProfitPerDiv),
                Tag = spec,
                Width = 95,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                DefaultCellStyle = { Format = ShowProfitPerDiv ? PROFIT_PER_DIV_FORMAT : PROFIT_PER_1M_FORMAT, Alignment = DataGridViewContentAlignment.MiddleRight }
            });
        }

        /// <summary>Rebuilds every row from the board in the current sort order, keeping the scroll position when possible.</summary>
        public void RefreshPriceGrid()
        {
            _refreshingGrid = true;
            int firstVisible = dgvPrices.FirstDisplayedScrollingRowIndex;

            dgvPrices.SuspendLayout();
            dgvPrices.Rows.Clear();
            foreach (var item in ItemsInGridOrder())
            {
                int index = dgvPrices.Rows.Add();
                DataGridViewRow row = dgvPrices.Rows[index];
                row.Tag = item;
                FillRow(row, item);
            }
            UpdateSortGlyphs();
            dgvPrices.ResumeLayout();

            if (firstVisible >= 0 && firstVisible < dgvPrices.Rows.Count)
            {
                dgvPrices.FirstDisplayedScrollingRowIndex = firstVisible;
            }
            _refreshingGrid = false;
        }

        /// <summary>Redraws the single row that shows <paramref name="item"/>.</summary>
        public void RefreshPriceGridRow(ItemReading item)
        {
            foreach (DataGridViewRow row in dgvPrices.Rows)
            {
                if (ReferenceEquals(row.Tag, item) || (row.Tag is ItemReading r && r.Name == item.Name))
                {
                    _refreshingGrid = true;
                    row.Tag = item;
                    FillRow(row, item);
                    _refreshingGrid = false;
                    return;
                }
            }
            RefreshPriceGrid(); // new item not in the grid yet
        }

        private void FillRow(DataGridViewRow row, ItemReading item)
        {
            row.Cells[COL_CATEGORY].Value = item.Category;
            row.Cells[COL_ITEM].Value = item.Name;
            foreach (DataGridViewColumn column in dgvPrices.Columns)
            {
                if (column.Tag is PriceField field)
                {
                    row.Cells[column.Index].Value = ShowTradeVolume ? VolumeFor(item, field) : item.Get(field);
                }
                else if (column.Tag is ProfitColumnSpec spec)
                {
                    row.Cells[column.Index].Value = spec.Value(item, ShowProfitPerDiv);
                }
            }
            row.Cells[COL_LAST_READ].Value = item.Timestamp == default
                ? ""
                : item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>Green for positive profit, red for negative, like the sheet's conditional formatting.</summary>
        private void dgvPrices_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvPrices.Columns[e.ColumnIndex].Tag is not ProfitColumnSpec) return;

            if (e.Value is double profit)
            {
                e.CellStyle.BackColor = profit >= 0 ? Color.FromArgb(198, 239, 206) : Color.FromArgb(255, 199, 206);
            }
            else
            {
                e.CellStyle.BackColor = SystemColors.Control;
            }
        }

        /// <summary>
        /// The user corrected a price. Recompute that row's profits, then mirror the correction to the
        /// sheet cell and append a record to the history so the next start shows the corrected value.
        /// </summary>
        private void dgvPrices_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_refreshingGrid || ShowTradeVolume || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvPrices.Columns[e.ColumnIndex].Tag is not PriceField field) return;
            if (dgvPrices.Rows[e.RowIndex].Tag is not ItemReading item) return;

            DataGridViewCell cell = dgvPrices.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (!TryParseCell(cell.Value, out double? newValue))
            {
                // Not a number: put the old value back.
                _refreshingGrid = true;
                cell.Value = item.Get(field);
                _refreshingGrid = false;
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            item.Set(field, newValue);
            item.Timestamp = DateTime.Now;
            _board.Recalculate(item);
            RefreshPriceGridRow(item);

            try
            {
                string sheetValue = newValue.HasValue ? newValue.Value.ToString(CultureInfo.InvariantCulture) : PricingChecker.UNREADABLE;
                _googleSheetUpdater.UpdateCell(PriceFields.SheetColumn(field) + item.Row, sheetValue);
                _history.Append(item, _board.Rates);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Price updated locally, but saving it failed:" + Environment.NewLine + ex.Message,
                    "Save price", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Accepts a number typed with either "." or the local decimal separator; blank clears the value.</summary>
        private static bool TryParseCell(object? raw, out double? value)
        {
            value = null;
            if (raw == null) return true;
            if (raw is double d) { value = d; return true; }

            string text = raw.ToString()?.Trim() ?? "";
            if (text.Length == 0) return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double invariant))
            {
                value = invariant;
                return true;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double local))
            {
                value = local;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Scan control
        // ------------------------------------------------------------------

        private void Start()
        {
            _started = true;
            _windowsUtil.SetStarted(true);
            _ = RefreshMarketDataAsync(); // once-per-hour guard inside; runs in the background while the scan clicks
            _pricingChecker.Start();
        }

        private void OnKeyEvent(Keys key, bool isDown, bool isControlDown)
        {
            if ((key == Keys.N) && !isDown && isControlDown)
            {
                if (!_started)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
            else if ((key == Keys.D0) && !isDown && isControlDown)
            {
                if (!_started)
                {
                    try
                    {
                        _pricingChecker.ScreenShotAndReadRatio();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void OnMouseKeyEvent(MouseButtons key, bool isDown)
        {

        }

        public void Stop()
        {
            _started = false;
            _windowsUtil.SetStarted(false);
            _pricingChecker.Stop();
        }

        public void AddOCRDebugControl(OCRDebug ocrDebug)
        {
            flpDebug.SuspendLayout();
            flpDebug.Controls.Add(ocrDebug);
            flpDebug.ResumeLayout();
            flpDebug.ScrollControlIntoView(ocrDebug);
        }

        public bool ShouldCheckChaos()
        {
            return chkCheckChaos.Checked;
        }

        public bool ShouldCheckExalt()
        {
            return chkCheckExalt.Checked;
        }

        private void MainLoop()
        {
            // This variable turn off all submodule from doing logic, but still let them to count cooldown
            int deltaTime = (int)(_stopwatch.Elapsed.TotalMilliseconds);
            _stopwatch.Restart();

            // Check for game focus
            if (_windowsUtil.GetCurrentWindowsProcessName() != "PathOfExile" && _started)
            {
                Stop();
            }

            if (_pricingChecker != null) _pricingChecker.MainLoop(deltaTime);
        }

        private void btnClearDebug_Click(object sender, EventArgs e)
        {
            flpDebug.Controls.Clear();
        }
    }
}
