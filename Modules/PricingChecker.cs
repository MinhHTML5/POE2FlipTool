using POE2FlipTool.DataModel;
using POE2FlipTool.Utilities;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;


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
        public const int DELAY_BETWEEN_ACTION_SHORT = 25;
        public const int DELAY_BETWEEN_ACTION_LONG = 75;
        public const int DELAY_BEFORE_SCREENSHOT = 500;

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

        public const string SELL_FOR_DIVINE_Y = "D";
        public const string BUY_WITH_EXALT_Y = "E";
        public const string BUY_WITH_CHAOS_Y = "G";
        public const string BUY_WITH_DIVINE_Y = "J";
        public const string SELL_FOR_EXALT_Y = "K";
        public const string SELL_FOR_CHAOS_Y = "M";
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

        public TradeItem itemExaltedOrb = new TradeItem("Exalted Orb", 0);
        public TradeItem itemChaosOrb = new TradeItem("Chaos Orb", 0);
        public TradeItem itemDivineOrb = new TradeItem("Divine Orb", 0);

        private TradeItem _processingItem = null;



        private bool _started = false;
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

        public List<(int, string)> GetItemList()
        {
            return _googleSheetUpdater.GetValueFromColumn("A");
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
            catch (Exception ex)
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

            List<(int, string)> items = GetItemList();

            // Here is where the check script begin
            // Select something on both side so the popular category show up
            MoveMouse(_iHavePoint.X, _iHavePoint.Y); SendLeftClick();
            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); SendLeftClick();
            MoveMouse(_iWantPoint.X, _iWantPoint.Y); SendLeftClick();
            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y); SendLeftClick();

            // Update div -> exalt value
            if (_main.ShouldCheckExalt())
            {
                ClickHave(itemDivineOrb);
                ClickWant(itemExaltedOrb);
                ScreenShotAndUpdateGoogleSheet(itemExaltedOrb, "B2");
            }

            // Update div -> chaos value
            if (_main.ShouldCheckChaos())
            {
                ClickWant(itemChaosOrb);
                ScreenShotAndUpdateGoogleSheet(itemChaosOrb, "B3");
            }

            // Go through each trade item and update trading value
            foreach (var (row, value) in items)
            {
                if (value.Contains("!!") || value.Length <= 0)
                {
                    continue;
                }

                var tradeItem = new TradeItem(value, row);
                // The code below is not inversed. For example, if we want to sell for divine
                // We search for "I want tradeItem" and "I have divine" to get the lowest price
                // someone else are willing to sell. That means we can sell around that price to.
                ClickHave(itemDivineOrb);
                ClickWant(tradeItem);
                ScreenShotAndUpdateGoogleSheet(tradeItem, SELL_FOR_DIVINE_Y + tradeItem.row, true);

                if (_main.ShouldCheckExalt())
                {
                    ClickHave(itemExaltedOrb);
                    ScreenShotAndUpdateGoogleSheet(tradeItem, SELL_FOR_EXALT_Y + tradeItem.row, true);
                }

                if (_main.ShouldCheckChaos())
                {
                    ClickHave(itemChaosOrb);
                    ScreenShotAndUpdateGoogleSheet(tradeItem, SELL_FOR_CHAOS_Y + tradeItem.row, true);
                }


                ClickHave(tradeItem);

                if (_main.ShouldCheckExalt())
                {
                    ClickWant(itemExaltedOrb);
                    ScreenShotAndUpdateGoogleSheet(tradeItem, BUY_WITH_EXALT_Y + tradeItem.row);
                }

                if (_main.ShouldCheckChaos())
                {
                    ClickWant(itemChaosOrb);
                    ScreenShotAndUpdateGoogleSheet(tradeItem, BUY_WITH_CHAOS_Y + tradeItem.row);
                }

                ClickWant(itemDivineOrb);
                ScreenShotAndUpdateGoogleSheet(tradeItem, BUY_WITH_DIVINE_Y + tradeItem.row);
            }
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

        public void TypeItemName(string name)
        {
            _commandQueue.Enqueue(new ActionCommand(() => Clipboard.SetText(name)));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.PressKey(Keys.V, true)));
            _commandQueue.Enqueue(new DelayCommand(DELAY_BETWEEN_ACTION_LONG));
        }

        
        public void ScreenShotAndUpdateGoogleSheet(TradeItem item, string cell, bool inverseScreenShotValue = false)
        {
            _commandQueue.Enqueue(new DelayCommand(DELAY_BEFORE_SCREENSHOT));
            _commandQueue.Enqueue(new ActionCommand(() => _googleSheetUpdater.UpdateCell(cell, ScreenShotAndGetCurrentTradeRatio(inverseScreenShotValue, item.name))));
        }

        public string ScreenShotAndGetCurrentTradeRatio(bool reverse = false, string itemName = "Custom")
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

            int splitIndex = 0;
            if (result.Contains(':'))
            {
                splitIndex = result.IndexOf(':');
            }
            else
            {
                return "=1/1";
            }

            string[] parts = result.Split(result[splitIndex]);
            if (parts.Length != 2)
            {
                return "=1/1";
            }


            float left = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float right = float.Parse(parts[1], CultureInfo.InvariantCulture);

            if ((right == 0 && !reverse) || (left == 0 && reverse))
            {
                return "=1/1";
            }

            string ratioString = "=" + (reverse ? (right + "/" + left) : (left + "/" + right));
            return ratioString;
        }
    }
}
