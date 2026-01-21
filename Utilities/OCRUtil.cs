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
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.LstmOnly);
            engine.DefaultPageSegMode = PageSegMode.SingleLine;
            engine.SetVariable("tessedit_char_whitelist", "0123456789:.");
            engine.SetVariable("classify_bln_numeric_mode", "1");
            engine.SetVariable("preserve_interword_spaces", "1");
            engine.SetVariable("tessedit_enable_dict_correction", "0");
            engine.SetVariable("textord_noise_area_ratio", "0.5");
            //engine.SetVariable("user_defined_dpi", "150");

            engine.SetVariable("load_system_dawg", "0");
            engine.SetVariable("load_freq_dawg", "0");

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