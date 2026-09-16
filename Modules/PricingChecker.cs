using POE2FlipTool.DataModel;
using POE2FlipTool.Utilities;
using System.Globalization;


namespace POE2FlipTool.Modules
{
    interface ICommand
    {
        bool Execute();
    }

    class DelayCommand : ICommand
    {
        private readonly int _delayMs;
        private DateTime _start;

        public DelayCommand(int delayMs)
        {
            _delayMs = delayMs;
        }

        public bool Execute()
        {
            if (_start == default)
                _start = DateTime.Now;

            return (DateTime.Now - _start).TotalMilliseconds >= _delayMs;
        }
    }

    class ActionCommand : ICommand
    {
        private readonly Action _action;
        private bool _done;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public bool Execute()
        {
            if (_done) return true;

            // Mark done first: if the action throws, the queue moves on instead of retrying the same
            // screenshot/parse on every tick until the user presses ctrl+N.
            _done = true;
            _action();
            return true;
        }
    }

    /// <summary>
    /// The "left:right" ratio shown by the currency exchange, e.g. 263:1.
    /// </summary>
    public readonly struct TradeRatio
    {
        public float Left { get; }
        public float Right { get; }

        public TradeRatio(float left, float right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Numeric value written to the sheet: left/right, or right/left when reversed.
        /// Null when either side is zero: the exchange never shows a 0 ratio, so that is an OCR miss.
        /// </summary>
        public double? Value(bool reverse)
        {
            if (Left <= 0 || Right <= 0) return null;
            float numerator = reverse ? Right : Left;
            float denominator = reverse ? Left : Right;
            return (double)numerator / denominator;
        }

        /// <summary>Formula string for the sheet, e.g. "=263/1", or "~" when the ratio is unusable.</summary>
        public string ToSheetFormula(bool reverse)
        {
            if (Value(reverse) == null) return PricingChecker.UNREADABLE;
            string n = (reverse ? Right : Left).ToString(CultureInfo.InvariantCulture);
            string d = (reverse ? Left : Right).ToString(CultureInfo.InvariantCulture);
            return "=" + n + "/" + d;
        }
    }

    public class PricingChecker
    {
        /// <summary>Sentinel written when OCR could not read a value; GoogleSheetUpdater skips it.</summary>
        public const string UNREADABLE = "~";

        public const int DELAY_BETWEEN_ACTION_SHORT = 25;
        public const int DELAY_BETWEEN_ACTION_LONG = 75;
        public const int DELAY_BEFORE_SCREENSHOT_SHORT = 200;
        public const int DELAY_BEFORE_SCREENSHOT_LONG = 600;

        public PointF OCR_TOP = new PointF(0.4692f, 0.17222223f);
        public PointF OCR_BOTTOM = new PointF(0.5338f, 0.192f);
        public PointF I_WANT = new PointF(0.36f, 0.22f);
        public PointF I_HAVE = new PointF(0.62f, 0.22f);
        public PointF REGEX = new PointF(0.5f, 0.87f);

        public PointF[] ITEM_SELECT = new PointF[]
        {
            new PointF(0.42f, 0.184f),
            new PointF(0.56f, 0.184f),
            new PointF(0.66f, 0.184f)
        };

        public float CATEGORY_HAVE_OFFSET_Y = 0.037f;

        public const float CATEGORY_ALL_X = 0.3f;
        public const float CATEGORY_ALL_Y = 0.15f;


        private Point _ocrTopPoint = new Point();
        private Point _ocrBottomPoint = new Point();
        private Point _iWantPoint = new Point();
        private Point _iHavePoint = new Point();
        private Point _regexPoint = new Point();


        private Point[] _itemSelectPoint = new Point[3];

        private int _categoryHaveOffsetY = 0;

        public Main _main;
        public WindowsUtil _windowsUtil;
        public InputHook _inputHook;
        public ColorUtil _colorUtil;
        public OCRUtil _ocrUtil;
        public GoogleSheetUpdater _googleSheetUpdater;
        private PriceBoard _board;
        private PriceHistoryWriter _historyWriter;

        public TradeItem itemExaltedOrb = new TradeItem("Exalted Orb", 0);
        public TradeItem itemChaosOrb = new TradeItem("Chaos Orb", 0);
        public TradeItem itemDivineOrb = new TradeItem("Divine Orb", 0);

        private bool _started = false;
        private Queue<ICommand> _commandQueue = new();


        public PricingChecker(Main main, WindowsUtil windowsUtil, InputHook inputHook, ColorUtil colorUtil, OCRUtil ocrUtil,
                              GoogleSheetUpdater googleSheetUpdater, PriceBoard board, PriceHistoryWriter historyWriter)
        {
            _main = main;
            _windowsUtil = windowsUtil;
            _inputHook = inputHook;
            _colorUtil = colorUtil;
            _ocrUtil = ocrUtil;
            _googleSheetUpdater = googleSheetUpdater;
            _board = board;
            _historyWriter = historyWriter;
        }

        public void Init()
        {
            _ocrTopPoint = _colorUtil.GetPixelPosition(OCR_TOP.X, OCR_TOP.Y);
            _ocrBottomPoint = _colorUtil.GetPixelPosition(OCR_BOTTOM.X, OCR_BOTTOM.Y);
            _iWantPoint = _colorUtil.GetPixelPosition(I_WANT.X, I_WANT.Y);
            _iHavePoint = _colorUtil.GetPixelPosition(I_HAVE.X, I_HAVE.Y);
            _regexPoint = _colorUtil.GetPixelPosition(REGEX.X, REGEX.Y);

            for (int i = 0; i < 3; i++)
            {
                _itemSelectPoint[i] = _colorUtil.GetPixelPosition(ITEM_SELECT[i].X, ITEM_SELECT[i].Y);
            }
            _categoryHaveOffsetY = _colorUtil.GetPixelPosition(0, CATEGORY_HAVE_OFFSET_Y).Y;
        }



        public void MainLoop(int deltaTime)
        {
            try
            {
                if (_commandQueue.Count == 0)
                {
                    _main.Stop();
                    return;
                }

                ICommand cmd = _commandQueue.Peek();
                if (cmd.Execute())
                    _commandQueue.Dequeue();
            }
            catch (Exception)
            {
            }
        }

        public void Stop()
        {
            _commandQueue.Clear();
            //_main.Stop(); - Never, never, ever, call this. It will cause a stack overflow.
        }

        public void Start()
        {
            _started = true;

            // Refresh the item list, gold costs and CONFIG rates from the sheet before scanning.
            _board.LoadFromSheet(_googleSheetUpdater.GetRows("A1:B"));
            _main.RefreshPriceGrid();

            // Here is where the check script begin
            // Select something on both side so the popular category show up
            MoveMouse(_iHavePoint.X, _iHavePoint.Y); SendLeftClick();
            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); SendLeftClick();
            MoveMouse(_iWantPoint.X, _iWantPoint.Y); SendLeftClick();
            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); SendLeftClick();

            // Update div -> exalt value. A fresh reading replaces the sheet value for this run's profit math.
            if (_main.ShouldCheckExalt())
            {
                ClickHave(itemDivineOrb);
                ClickWant(itemExaltedOrb);
                ScreenShotAndRecord(itemExaltedOrb.name, PriceBoard.DIV_TO_EX_CELL, false, false,
                    v => { if (v.HasValue) { _board.Rates.DivToEx = v; _main.OnRatesChanged(); } });
            }

            // Update div -> chaos value
            if (_main.ShouldCheckChaos())
            {
                ClickWant(itemChaosOrb);
                ScreenShotAndRecord(itemChaosOrb.name, PriceBoard.DIV_TO_CHAOS_CELL, false, false,
                    v => { if (v.HasValue) { _board.Rates.DivToChaos = v; _main.OnRatesChanged(); } });
            }

            // Go through each trade item and update trading value. Only checked categories are scanned;
            // items above the first category header (no category) are always scanned.
            HashSet<string> enabledCategories = _main.GetEnabledCategories();
            foreach (var item in _board.Items)
            {
                if (item.Category.Length > 0 && !enabledCategories.Contains(item.Category))
                {
                    continue;
                }

                var tradeItem = new TradeItem(item.Name, item.Row);
                var reading = new ItemReading(item.Name, item.Row, item.Category) { GoldCost = item.GoldCost };

                // The code below is not inversed. For example, if we want to sell for divine
                // We search for "I want tradeItem" and "I have divine" to get the lowest price
                // someone else are willing to sell. That means we can sell around that price to.
                ClickHave(itemDivineOrb);
                ClickWant(tradeItem);
                ScreenShotAndRecord(reading, PriceField.SellForDiv, true, false);
                ClickFlip();
                ScreenShotAndRecord(reading, PriceField.BuyWithDiv, false, true);

                if (_main.ShouldCheckExalt())
                {
                    ClickFlip();
                    ClickHave(itemExaltedOrb);
                    ScreenShotAndRecord(reading, PriceField.SellForEx, true, false);
                    ClickFlip();
                    ScreenShotAndRecord(reading, PriceField.BuyWithEx, false, true);
                }

                if (_main.ShouldCheckChaos())
                {
                    ClickFlip();
                    ClickHave(itemChaosOrb);
                    ScreenShotAndRecord(reading, PriceField.SellForChaos, true, false);
                    ClickFlip();
                    ScreenShotAndRecord(reading, PriceField.BuyWithChaos, false, true);
                }

                // All prices for this item are in: log it, merge it into the board and refresh the grid row.
                _commandQueue.Enqueue(new ActionCommand(() => FinishReading(reading)));
            }
        }

        private void FinishReading(ItemReading reading)
        {
            // A failed buy read takes this run's sell value of the same currency (and vice versa).
            // The borrowed value also goes to the sheet cell that the failed read left untouched.
            foreach (PriceField borrowed in reading.BorrowMissingPairValues())
            {
                double value = reading.Get(borrowed)!.Value;
                _googleSheetUpdater.UpdateCell(PriceFields.SheetColumn(borrowed) + reading.Row, value.ToString(CultureInfo.InvariantCulture));
            }

            bool readAnything = PriceFields.All.Any(f => reading.Get(f).HasValue);

            reading.Timestamp = DateTime.Now;
            if (readAnything)
            {
                // The CSV keeps the raw result: blanks mark prices that could not be read this run.
                ProfitCalculator.Fill(reading, _board.Rates);
                _historyWriter.Append(reading, _board.Rates);
            }

            // Merge into the board: fields this run could not read keep their previous value.
            ItemReading merged = _board.Apply(reading);
            _main.RefreshPriceGridRow(merged);
        }

        public void ClickWant(TradeItem want)
        {
            MoveMouse(_iWantPoint.X, _iWantPoint.Y);
            SendLeftClick();
            Point catAll = _colorUtil.GetPixelPosition(CATEGORY_ALL_X, CATEGORY_ALL_Y);
            MoveMouse(catAll.X, catAll.Y);
            SendLeftClick();
            MoveMouse(_regexPoint.X, _regexPoint.Y);
            SendLeftClick();
            TypeItemName(want.name);
            MoveMouse(_itemSelectPoint[want.itemSelectIndex].X, _itemSelectPoint[want.itemSelectIndex].Y);
            SendLeftClick();
        }

        public void ClickHave(TradeItem have)
        {
            MoveMouse(_iHavePoint.X, _iHavePoint.Y);
            SendLeftClick();
            Point catAll = _colorUtil.GetPixelPosition(CATEGORY_ALL_X, CATEGORY_ALL_Y);
            MoveMouse(catAll.X, catAll.Y);
            SendLeftClick();
            MoveMouse(_regexPoint.X, _regexPoint.Y);
            SendLeftClick();
            TypeItemName(have.name);
            MoveMouse(_itemSelectPoint[have.itemSelectIndex].X, _itemSelectPoint[have.itemSelectIndex].Y);
            SendLeftClick();
        }

        public void ClickFlip()
        {
            MoveMouse(_iWantPoint.X, _iWantPoint.Y);
            SendLeftClickWithControl();
        }


        public void Sleep(int milliseconds)
        {
            _commandQueue.Enqueue(new DelayCommand(milliseconds));
        }

        public void MoveMouse(int x, int y)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.MoveMouse(x, y)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION_SHORT));
        }

        public void SendLeftClick()
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendLeftClick()));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION_LONG));
        }

        public void SendLeftClickWithControl()
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendKeyDown(Keys.ControlKey)));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendLeftClick()));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendKeyUp(Keys.ControlKey)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION_LONG));
        }

        public void TypeItemName(string name)
        {
            _commandQueue.Enqueue(new ActionCommand(() => Clipboard.SetText(name)));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.PressKey(Keys.V, true)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION_LONG));
        }


        /// <summary>Reads one price of an item and stores it both in the reading and in the item's sheet cell.</summary>
        public void ScreenShotAndRecord(ItemReading reading, PriceField field, bool reverse, bool delayShort)
        {
            string cell = PriceFields.SheetColumn(field) + reading.Row;
            ScreenShotAndRecord(reading.Name, cell, reverse, delayShort, v => reading.Set(field, v));
        }

        /// <summary>
        /// Waits for the exchange UI to settle, OCRs the ratio, hands the numeric value to <paramref name="store"/>
        /// and writes the same value to the sheet cell as a formula.
        /// When the ratio cannot be read (null, or a zero on either side) nothing happens at all: the store
        /// callback is not invoked, so the previous value stays in place, and the sheet cell is left untouched.
        /// </summary>
        public void ScreenShotAndRecord(string itemName, string cell, bool reverse, bool delayShort, Action<double?> store)
        {
            _commandQueue.Enqueue(new DelayCommand(delayShort ? DELAY_BEFORE_SCREENSHOT_SHORT : DELAY_BEFORE_SCREENSHOT_LONG));
            _commandQueue.Enqueue(new ActionCommand(() =>
            {
                TradeRatio? ratio = ScreenShotAndReadRatio(itemName);
                double? value = ratio?.Value(reverse);
                if (!value.HasValue)
                {
                    return; // unreadable: keep the old reading, do not touch the sheet
                }

                store(value);
                _googleSheetUpdater.UpdateCell(cell, ratio!.Value.ToSheetFormula(reverse));
            }));
        }

        /// <summary>
        /// Screenshots the ratio strip, runs the template OCR and parses "left:right". Null when unreadable.
        /// </summary>
        public TradeRatio? ScreenShotAndReadRatio(string itemName = "Custom")
        {
            Bitmap bitmap = _ocrUtil.PrintScreenAt(_ocrTopPoint, _ocrBottomPoint);
            bitmap = _ocrUtil.UpScale(bitmap, 2);
            bitmap = _ocrUtil.ToGrayscale(bitmap);
            bitmap = _ocrUtil.IncreaseContrast(bitmap, 2f);
            bitmap = _ocrUtil.Threshold(bitmap, 120);
            bitmap = _ocrUtil.Invert(bitmap);

            string result = "";
            List<Bitmap> chars = _ocrUtil.SplitCharacters(bitmap);

            for (int i = 0; i < chars.Count; i++)
            {
                string charResult = _ocrUtil.RecognizeCharacter(chars[i]);
                result += charResult;
            }

            OCRDebug ocrDebug = new OCRDebug();
            ocrDebug.Init(itemName, bitmap, result);
            _main.AddOCRDebugControl(ocrDebug);

            string[] parts = result.Split(':');
            if (parts.Length != 2)
            {
                return null;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float left) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float right))
            {
                return null;
            }

            return new TradeRatio(left, right);
        }
    }
}
