using POE2FlipTool.Modules;
using POE2FlipTool.Utilities;

namespace POE2FlipTool
{
    public partial class Main : Form
    {
        public static Main sInstance;

        private WindowsUtil _windowsUtil;
        private InputHook _inputHook;
        private ColorUtil _colorUtil;
        private OCRUtil _ocrUtil;

        private PricingChecker _pricingChecker;

        private bool _started = false;

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

            _pricingChecker = new PricingChecker(this, _windowsUtil, _inputHook, _colorUtil);
        }

        private void Start()
        {
            _started = true;
            _windowsUtil.SetStarted(true);

            _pricingChecker.StartCheck();
        }

        private void OnKeyEvent(Keys key, bool isDown, bool isControlDown)
        {
            if ((key == Keys.Enter) && !isDown && isControlDown)
            {
                if (!_started)
                {
                    Start();
                }
            }
        }

        private void OnMouseKeyEvent(MouseButtons key, bool isDown)
        {
            
        }
    }
}
