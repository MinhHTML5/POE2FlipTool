using ninja.poe;
using POE2FlipTool.DataModel;
using POE2FlipTool.Utilities;
using System.Globalization;
using Windows.Foundation.Diagnostics;


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

            _action();
            _done = true;
            return true;
        }
    }
















    public class PricingChecker
    {
        public const int DELAY_BETWEEN_ACTION = 250;
        public const int DELAY_BEFORE_SCREENSHOT = 500;
        public const int DELAY_AFTER_MOVEMOUSE = 1000;

        public PointF OCR_AVAIL_TOP = new PointF(0.283f, 0.22f);
        public PointF OCR_AVAIL_BOTTOM = new PointF(0.365f, 0.345f);
        public PointF I_WANT = new PointF(0.19f, 0.22f);
        public PointF I_HAVE = new PointF(0.44f, 0.22f);
        public PointF REGEX = new PointF(0.36f, 0.87f);
        public PointF TRADE_DETAIL = new PointF(0.37f, 0.16f);

        public PointF[] ITEM_SELECT = new PointF[]
        {
            new PointF(0.273f, 0.184f),
            new PointF(0.375f, 0.184f),
            new PointF(0.473f, 0.184f)
        };

        public string[] CORE_CURRENCIES = new string[]
        {
            "Exalted Orb",
            "Chaos Orb",
            "Divine Orb"
        };

        public float CATEGORY_HAVE_OFFSET_Y = 0.037f;

        public const int SELL_FOR_DIVINE_Y = 11;
        public const int BUY_WITH_EXALT_Y = 13;
        public const int BUY_WITH_CHAOS_Y = 16;
        public const int BUY_WITH_DIVINE_Y = 30;
        public const int SELL_FOR_EXALT_Y = 32;
        public const int SELL_FOR_CHAOS_Y = 35;



        private List<TradeItem> _items = new List<TradeItem>();
        private List<TradeCategory> _categories = new List<TradeCategory>();
        private List<TradedItem> _poeNinjaItems = new List<TradedItem>();
        private SheetConfig _sheetConfig = new SheetConfig(); // Can be overridden by config file

        private Point _ocrAvailableTopPoint = new Point();
        private Point _ocrAvailableBottomPoint = new Point();
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

        public TradeItem itemExaltedOrb = new TradeItem("Currency", "Exalted Orb");
        public TradeItem itemChaosOrb = new TradeItem("Currency", "Chaos Orb");
        public TradeItem itemDivineOrb = new TradeItem("Currency", "Divine Orb");
        public List<MarketValue> _marketValues = new List<MarketValue>();

        private Queue<ICommand> _commandQueue = new();


        public PricingChecker(Main main, WindowsUtil windowsUtil, InputHook inputHook, ColorUtil colorUtil, OCRUtil ocrUtil, GoogleSheetUpdater googleSheetUpdater)
        {
            _main = main;
            _windowsUtil = windowsUtil;
            _inputHook = inputHook;
            _colorUtil = colorUtil;
            _ocrUtil = ocrUtil;
            _googleSheetUpdater = googleSheetUpdater;
        }

        public void Init()
        {
            _ocrAvailableTopPoint = _colorUtil.GetPixelPosition(OCR_AVAIL_TOP.X, OCR_AVAIL_TOP.Y);
            _ocrAvailableBottomPoint = _colorUtil.GetPixelPosition(OCR_AVAIL_BOTTOM.X, OCR_AVAIL_BOTTOM.Y);
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
            if (_commandQueue.Count == 0)
            {
                _main.Stop();
                return;
            }

            ICommand cmd = _commandQueue.Peek();
            try
            {
                if (cmd.Execute())
                    _commandQueue.Dequeue();
            }
            catch (Exception ex)
            {
                _commandQueue.Dequeue();
            }
        }

        public void Stop()
        {
            _commandQueue.Clear();
            //_main.Stop(); - Never, never, ever, call this. It will cause a stack overflow.
        }
        public void Start()
        {
            _items = ConfigReader.ReadItemConfig();
            _categories = ConfigReader.ReadCategoryConfig();
            _poeNinjaItems = ConfigReader.GetPoeNinjaList(ConfigReader.poeConfig) ?? new List<TradedItem>();

            // Here is where the check script begin
            // Select something on both side so the popular category show up
            CommandMoveMouse(_iHavePoint.X, _iHavePoint.Y); CommandSendLeftClick();
            CommandMoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); CommandSendLeftClick();
            CommandMoveMouse(_iWantPoint.X, _iWantPoint.Y); CommandSendLeftClick();
            CommandMoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); CommandSendLeftClick();

            // Update div -> exalt value
            ClickHave(itemDivineOrb);
            ClickWant(itemExaltedOrb);
            CommandGetTradeRatio(itemExaltedOrb.name, itemDivineOrb.name);
            //Update div -> chaos value
            ClickWant(itemChaosOrb);
            CommandGetTradeRatio(itemChaosOrb.name, itemDivineOrb.name);

            // Go through each trade item and update trading value
            //for (int i = 0; i < _items.Count; i++)

            int operatingLine = _sheetConfig.StatingRow;
            foreach (var item in _poeNinjaItems)
            {
                // Skip core currencies
                if (CORE_CURRENCIES.Contains(item.Name)) continue;

                PriceCheck(item);

                operatingLine++;
            }
        }

        private void PriceCheck(TradedItem item)
        {
            var tradeItem = item.ToTradeItem();

            ClickWant(tradeItem);
            ClickHave(itemDivineOrb);
            CommandGetTradeRatio(item.Name, itemDivineOrb.name);
            ClickHave(itemExaltedOrb);
            CommandGetTradeRatio(item.Name, itemExaltedOrb.name);
            ClickHave(itemChaosOrb);
            CommandGetTradeRatio(item.Name, itemChaosOrb.name);
        }

        public void ClickWant(TradeItem want)
        {
            CommandMoveMouse(_iWantPoint.X, _iWantPoint.Y);
            CommandSendLeftClick();
            CommandMoveMouse(GetCategoryCoord(want.category));
            CommandSendLeftClick();
            CommandMoveMouse(_regexPoint.X, _regexPoint.Y);
            CommandSendLeftClick();
            CommandTypeItemName(want.name);
            CommandMoveMouse(_itemSelectPoint[want.itemSelectIndex].X, _itemSelectPoint[want.itemSelectIndex].Y);
            CommandSendLeftClick();
        }

        public void ClickHave(TradeItem have)
        {
            CommandMoveMouse(_iHavePoint.X, _iHavePoint.Y);
            CommandSendLeftClick();
            var catCoord = GetCategoryCoord(have.category);
            CommandMoveMouse(catCoord.X, catCoord.Y + _categoryHaveOffsetY);
            CommandSendLeftClick();
            CommandMoveMouse(_regexPoint.X, _regexPoint.Y);
            CommandSendLeftClick();
            CommandTypeItemName(have.name);
            CommandMoveMouse(_itemSelectPoint[have.itemSelectIndex].X, _itemSelectPoint[have.itemSelectIndex].Y);
            CommandSendLeftClick();
        }


        public void MoveMouse(Point p)
        {
            Random random = new Random();
            var rndIntX = random.Next(-5, 5);
            var rndIntY = random.Next(-2, 2);
            _inputHook.MoveMouse(p.X + rndIntX, p.Y + rndIntY);
        }

        public void CommandSleep(int milliseconds)
        {
            _commandQueue.Enqueue(new DelayCommand(milliseconds));
        }

        public void CommandMoveMouse(Point p)
        {
            // Add some random offset to avoid detection
            Random random = new Random();
            var rndIntX = random.Next(-5, 5);
            var rndIntY = random.Next(-2, 2);
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.MoveMouseSmooth(p.X + rndIntX, p.Y + rndIntY)));
        }

        public void CommandMoveMouse(int x, int y)
        {
            // Add some random offset to avoid detection
            Random random = new Random();
            var rndIntX = random.Next(-5, 5);
            var rndIntY = random.Next(-2, 2);
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.MoveMouseSmooth(x + rndIntX, y + rndIntY)));
        }

        public void CommandSendLeftClick(bool ctrl = false)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendLeftClick(ctrl)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION));
        }

        public void CommandSendKeyDown(Keys key)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendKeyDown(key)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION));
        }

        public void CommandSendKeyUp(Keys key)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendKeyUp(key)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION));
        }

        public void CommandTypeItemName(string name)
        {
            _commandQueue.Enqueue(new ActionCommand(() => Clipboard.SetText(name)));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.PressKey(Keys.V, true)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION));
        }


        //public void ScreenShotAndUpdateGoogleSheet(string itemName, string cell, bool inverseScreenShotValue = false)
        //{
        //    _commandQueue.Enqueue(new DelayCommand(DELAY_BEFORE_SCREENSHOT));
        //    _commandQueue.Enqueue(new ActionCommand(() => _googleSheetUpdater.UpdateCell(cell, ScreenShotAndGetCurrentTradeRatio(inverseScreenShotValue, itemName))));
        //}

        public void CommandUpdateGoogleSheet(string cell, string value)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _googleSheetUpdater.UpdateCell(cell, value)));
        }

        public void CommandGetTradeRatio(string buyItem = "Custom", string sellItem = "Custom")
        {
            CommandMoveMouse(_iHavePoint.X, _iHavePoint.Y);
            CommandSendLeftClick(true);
            CommandSendLeftClick(true);
            _commandQueue.Enqueue(new DelayCommand(DELAY_BEFORE_SCREENSHOT));

            MarketValue marketValue = new MarketValue()
            {
                ItemBuyName = buyItem,
                ItemSellName = sellItem
            };

            CommandMoveMouse(_colorUtil.GetPixelPosition(TRADE_DETAIL));
            _commandQueue.Enqueue(new DelayCommand(DELAY_AFTER_MOVEMOUSE));
            _commandQueue.Enqueue(new ActionCommand(() => { marketValue.AvailableRate = ScreenShotAndGetCurrentTradeRatio(buyItem, sellItem); }));
            CommandMoveMouse(_iHavePoint.X, _iHavePoint.Y);
            CommandSendLeftClick(true);
            CommandMoveMouse(_colorUtil.GetPixelPosition(TRADE_DETAIL));
            _commandQueue.Enqueue(new DelayCommand(DELAY_AFTER_MOVEMOUSE));
            _commandQueue.Enqueue(new ActionCommand(() => { marketValue.CompetingRate = ScreenShotAndGetCurrentTradeRatio(buyItem, sellItem); }));
            _commandQueue.Enqueue(new ActionCommand(() => _marketValues.Add(marketValue)));
            CommandMoveMouse(_iWantPoint.X, _iWantPoint.Y);
            CommandSendLeftClick(true);
        }

        public List<(float, float)> ScreenShotAndGetCurrentTradeRatio(string buyItem = "Custom", string sellItem = "Custom")
        {
            Bitmap bitmap = _ocrUtil.PrintScreenAt(_ocrAvailableTopPoint, _ocrAvailableBottomPoint);
            return ExtractMarketValue(buyItem, sellItem, bitmap);
        }

        private List<(float, float)> ExtractMarketValue(string buyItem, string sellItem, Bitmap bitmap)
        {
            List<(float, float)> rates = new List<(float, float)>();
            bitmap = ProcessBitmap(bitmap);
            List<Bitmap> bitmaps = _ocrUtil.SplitGrid(bitmap, 6, 1);
            bitmap.Save(@"debug\debug_full.png");
            int count = 0;
            foreach (var bmp in bitmaps)
            {
                bmp.Save($@"debug\debug_split{count}.png");
                count++;
                var ratio = ExtractRatio(false, buyItem + " to " + sellItem, bmp);
                rates.Add(ratio);
            }

            return rates;
        }

        private Bitmap ProcessBitmap(Bitmap bitmap)
        {
            bitmap = _ocrUtil.ToGrayscale(bitmap);
            bitmap = _ocrUtil.UpScale(bitmap, 2);
            //bitmap = _ocrUtil.IncreaseContrast(bitmap, 2f);
            bitmap = _ocrUtil.Threshold(bitmap, 160);
            bitmap = _ocrUtil.Invert(bitmap);
            return bitmap;
        }

        private (float, float) ExtractRatio(bool reverse, string itemName, Bitmap bitmap)
        {
            string result = "";
            var splits = _ocrUtil.SplitGrid(bitmap, 1, 2);
            splits[0].Save(@"debug\debug_extractratio0.png");
            splits[1].Save(@"debug\debug_extractratio1.png");

            List<Bitmap> chars = _ocrUtil.SplitCharacters(splits[0]);

            for (int i = 0; i < chars.Count; i++)
            {
                string charResult = _ocrUtil.RecognizeCharacter(chars[i]);
                result += charResult;
            }

            OCRDebug ocrDebug = new OCRDebug();
            ocrDebug.Init(itemName, bitmap, result);
            _main.AddOCRDebugControl(ocrDebug);

            int splitIndex = 0;
            if (result.Contains(':'))
            {
                splitIndex = result.IndexOf(':');
            }
            else
            {
                throw new FormatException("Invalid text: " + result);
            }

            string[] parts = result.Split(result[splitIndex]);
            if (parts.Length != 2)
            {
                throw new FormatException("Invalid text: " + result);
            }


            float left = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float right = float.Parse(parts[1], CultureInfo.InvariantCulture);

            if ((right == 0 && !reverse) || (left == 0 && reverse))
            {
                throw new DivideByZeroException();
            }

            float ratio = reverse ? (right / left) : (left / right);

            result = "";
            List<Bitmap> chars2 = _ocrUtil.SplitCharacters(splits[1]);
            for (int i = 0; i < chars2.Count; i++)
            {
                string charResult = _ocrUtil.RecognizeCharacter(chars2[i]);
                result += charResult;
            }
            float stock = float.Parse(result, CultureInfo.InvariantCulture);

            return (ratio, stock);
        }

        public Point GetCategoryCoord(string category)
        {
            var cat = _categories.Find(c => c.name == category);
            if (cat == null)
            {
                throw new Exception("Category not found : " + category);
            }
            return _colorUtil.GetPixelPosition(cat.x, cat.y);
        }
    }
}
