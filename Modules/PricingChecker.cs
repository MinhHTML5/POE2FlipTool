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
        public const int SLEEP_TIME = 250;
        public const int SLEEP_TIME_WAIT = 750;

        public PointF OCR_TOP = new PointF(0.30645312f, 0.17222223f);
        public PointF OCR_BOTTOM = new PointF(0.34664064f, 0.192f);
        public PointF I_WANT = new PointF(0.19f, 0.22f);
        public PointF I_HAVE = new PointF(0.44f, 0.22f);
        public PointF REGEX = new PointF(0.36f, 0.87f);

        public PointF[] ITEM_SELECT = new PointF[]
        {
            new PointF(0.273f, 0.184f),
            new PointF(0.375f, 0.184f),
            new PointF(0.473f, 0.184f)
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

        public TradeItem itemExaltedOrb = new TradeItem("Currency", "Exalted Orb", 0, "B");
        public TradeItem itemChaosOrb = new TradeItem("Currency", "Chaos Orb", 0, "B");
        public TradeItem itemDivineOrb = new TradeItem("Currency", "Divine Orb", 1, "B");




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
                _commandQueue.Clear();
                _main.Stop();
                _main.SetErrorMessage(ex.Message);
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
            _items = ConfigReader.ReadItemConfig();
            _categories = ConfigReader.ReadCategoryConfig();

            TradeCategory category = _categories.Find(c => c.name == itemExaltedOrb.category);
            itemExaltedOrb.categoryCoord = _colorUtil.GetPixelPosition(category.x, category.y);
            category = _categories.Find(c => c.name == itemChaosOrb.category);
            itemChaosOrb.categoryCoord = _colorUtil.GetPixelPosition(category.x, category.y);
            category = _categories.Find(c => c.name == itemDivineOrb.category);
            itemDivineOrb.categoryCoord = _colorUtil.GetPixelPosition(category.x, category.y);

            for (int i = 0; i < (int)_items.Count; i++)
            {
                TradeItem tradeItem = _items[i];
                category = _categories.Find(c => c.name == tradeItem.category);
                if (category != null)
                {
                    tradeItem.categoryCoord = _colorUtil.GetPixelPosition(category.x, category.y);
                }
                else
                {
                    throw new Exception("Category not found for item: " + tradeItem.name);
                }
            }

            // Here is where the check script begin
            // Select something on both side
            MoveMouse(_iHavePoint.X, _iHavePoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);

            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);

            MoveMouse(_iWantPoint.X, _iWantPoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);

            MoveMouse(_itemSelectPoint[0].X, _itemSelectPoint[0].Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();

            // Update div -> exalt value
            ClickWantHave(itemExaltedOrb, itemDivineOrb);
            Sleep(SLEEP_TIME_WAIT);
            ScreenShotAndUpdateGoogleSheet("B1");

            // Update div -> chaos value
            ClickWantHave(itemChaosOrb, itemDivineOrb);
            Sleep(SLEEP_TIME_WAIT);
            ScreenShotAndUpdateGoogleSheet("B2");

            // Go through each trade item and update trading value
            for(int i = 0; i < _items.Count; i++)
            {
                TradeItem tradeItem = _items[i];

                // The code below is not inversed. For example, if we want to sell for divine
                // We search for "I want tradeItem" and "I have divine" to get the lowest price
                // someone else are willing to sell. That means we can sell around that price to.

                ClickWantHave(tradeItem, itemDivineOrb);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + SELL_FOR_DIVINE_Y, true);

                ClickWantHave(itemExaltedOrb, tradeItem);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + BUY_WITH_EXALT_Y);

                ClickWantHave(itemChaosOrb, tradeItem);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + BUY_WITH_CHAOS_Y);

                ClickWantHave(itemDivineOrb, tradeItem);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + BUY_WITH_DIVINE_Y);

                ClickWantHave(tradeItem, itemExaltedOrb);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + SELL_FOR_EXALT_Y, true);

                ClickWantHave(tradeItem, itemChaosOrb);
                Sleep(SLEEP_TIME_WAIT);
                ScreenShotAndUpdateGoogleSheet(tradeItem.column + SELL_FOR_CHAOS_Y, true);
            }
        }


        

        public void ClickWantHave(TradeItem want, TradeItem have) 
        {
            Sleep(SLEEP_TIME);

            // ================================================================================================================
            MoveMouse(_iHavePoint.X, _iHavePoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(have.categoryCoord.X, have.categoryCoord.Y + _categoryHaveOffsetY);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(_regexPoint.X, _regexPoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            TypeItemName(have.name);
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(_itemSelectPoint[have.itemSelectIndex].X, _itemSelectPoint[have.itemSelectIndex].Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================

            // ================================================================================================================
            MoveMouse(_iWantPoint.X, _iWantPoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(want.categoryCoord.X, want.categoryCoord.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(_regexPoint.X, _regexPoint.Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            TypeItemName(want.name);
            Sleep(SLEEP_TIME);
            // ================================================================================================================
            MoveMouse(_itemSelectPoint[want.itemSelectIndex].X, _itemSelectPoint[want.itemSelectIndex].Y);
            Sleep(SLEEP_TIME);
            SendLeftClick();
            Sleep(SLEEP_TIME);
            // ================================================================================================================
        }

        public void Sleep(int milliseconds)
        {
            _commandQueue.Enqueue(new DelayCommand(milliseconds));
        }

        public void MoveMouse(int x, int y)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.MoveMouse(x, y)));
        }

        public void SendLeftClick()
        {
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.SendLeftClick()));
        }

        public void TypeItemName(string name)
        {
            _commandQueue.Enqueue(new ActionCommand(() => Clipboard.SetText(name)));
            _commandQueue.Enqueue(new ActionCommand(() => _inputHook.PressKey(Keys.V, true)));
        }

        
        public void ScreenShotAndUpdateGoogleSheet(string cell, bool inverseScreenShotValue = false)
        {
            _commandQueue.Enqueue(new ActionCommand(() => _googleSheetUpdater.UpdateCell(cell, ScreenShotAndGetCurrentTradeRatio(inverseScreenShotValue))));
        }

        public string ScreenShotAndGetCurrentTradeRatio(bool reverse = false)
        {
            Bitmap bitmap = _ocrUtil.PrintScreenAt(_ocrTopPoint, _ocrBottomPoint);
            bitmap = _ocrUtil.UpScale(bitmap, 2);
            bitmap = _ocrUtil.ToGrayscale(bitmap);
            bitmap = _ocrUtil.IncreaseContrast(bitmap, 2f);
            bitmap = _ocrUtil.Threshold(bitmap, 100);
            bitmap = _ocrUtil.Invert(bitmap);
            
            _main.SetDebugOCRResult(bitmap, "");

            string result = "";
            List<Bitmap> chars = _ocrUtil.SplitCharacters(bitmap);

            for (int i = 0; i < chars.Count; i++)
            {
                string charResult = _ocrUtil.RecognizeCharacter(chars[i]);
                result += charResult;

                //OCRDebug ocrDebug = new OCRDebug();
                //ocrDebug.Init(chars[i], charResult);
                //_main.AddOCRDebugControl(ocrDebug);
            }

            OCRDebug ocrDebug = new OCRDebug();
            ocrDebug.Init(bitmap, result);
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

            string ratioString = "=" + (reverse ? (right + "/" + left) : (left + "/" + right));
            _main.SetErrorMessage(result + "    GG Formula: \"" + ratioString + "\"");
            return ratioString;
        }
    }
}
