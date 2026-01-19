using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using Tesseract;

namespace POE2FlipTool.Utilities
{
    public class OCRUtil
    {
        public static string OCRAsync(Bitmap bitmap)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractAndLstm);
            engine.DefaultPageSegMode = PageSegMode.SingleLine;
            engine.SetVariable("tessedit_char_whitelist", "0123456789.:");

            using Pix pix = bitmap.ToPix();
            using Page page = engine.Process(pix);

            string text = page.GetText();
            return text;
        }
    }
}


public static class TesseractExtensions
{
    public static Pix ToPix(this Bitmap bitmap)
    {
        return PixConverter.ToPix(bitmap);
    }
}