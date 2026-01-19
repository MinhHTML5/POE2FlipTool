using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using POE2FlipTool.Utilities;

namespace POE2FlipTool.Modules
{
    internal class PricingChecker
    {
        public const float OCR_TOP_X = 0.28945312f;
        public const float OCR_TOP_Y = 0.17222223f;
        public const float OCR_BOTTOM_X = 0.35664064f;
        public const float OCR_BOTTOM_Y = 0.18888889f;


        private Point _ocrTopPoint = new Point();
        private Point _ocrBottomPoint = new Point();


        public Main _main;
        public WindowsUtil _windowsUtil;
        public InputHook _inputHook;
        public ColorUtil _colorUtil;

        public PricingChecker(Main main, WindowsUtil windowsUtil, InputHook inputHook, ColorUtil colorUtil) 
        {
            _main = main;
            _windowsUtil = windowsUtil;
            _inputHook = inputHook;
            _colorUtil = colorUtil;
        }

        public async void StartCheck()
        {
            Bitmap bitmap = _colorUtil.PrintScreenAt(_ocrTopPoint, _ocrBottomPoint);
            try
            {
                string temp = await OCRUtil.OCRAsync(bitmap);

            }
            catch (Exception ex)
            {

            }
        }
    }
}
