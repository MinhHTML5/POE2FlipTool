using POE2FlipTool.Modules;
using POE2FlipTool.Utilities;
using System.Diagnostics;
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

        private GoogleSheetUpdater _googleSheetUpdater;
        private PricingChecker _pricingChecker;

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

        private void Main_Load(object sender, EventArgs e)
        {
            _inputHook.RegisterRawInputDevices(this.Handle, OnMouseKeyEvent, OnKeyEvent);

            _timer.Interval = UPDATE_INTERVAL;
            _timer.Tick += (s, e) => MainLoop();
            _timer.Start();
            _stopwatch.Start();

            _googleSheetUpdater = new GoogleSheetUpdater();
            _pricingChecker = new PricingChecker(this, _windowsUtil, _inputHook, _colorUtil, _googleSheetUpdater);
            _pricingChecker.Init();
        }

        private void Start()
        {
            _started = true;
            _windowsUtil.SetStarted(true);
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
                        _pricingChecker.ScreenShotAndGetCurrentTradeRatio();
                    }
                    catch (Exception ex)
                    {
                        SetErrorMessage(ex.Message);
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

        public void SetDebugOCRResult(Bitmap bitmap, string text)
        {
            pctDebug.Image = bitmap;
            lblDebug.Text = text;
        }

        public void SetErrorMessage(string message)
        {
            lblDebug.Text = message;
        }


        private void MainLoop()
        {
            // This variable turn off all submodule from doing logic, but still let them to count cooldown
            int deltaTime = (int)(_stopwatch.Elapsed.TotalMilliseconds);
            _stopwatch.Restart();

            // Check for game focus
            if (_windowsUtil.GetCurrentWindowsProcessName() != "PathOfExile" && _started)
            {
                
            }

            _pricingChecker.MainLoop(deltaTime);
        }
    }
}
