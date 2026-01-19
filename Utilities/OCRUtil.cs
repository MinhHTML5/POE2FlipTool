using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Drawing.Imaging;

namespace POE2FlipTool.Utilities
{
    public class OCRUtil
    {
        public static async Task<string> OCRAsync(Bitmap bitmap)
        {
            using var stream = new InMemoryRandomAccessStream();
            bitmap.Save(stream.AsStream(), ImageFormat.Bmp);
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            var result = await engine.RecognizeAsync(softwareBitmap);

            return result.Text;
        }
    }
}