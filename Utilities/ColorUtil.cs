using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POE2FlipTool.Utilities
{
    public class ColorUtil
    {
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
        IntPtr hdcDest, int nXDest, int nYDest,
        int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc,
        int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int SRCCOPY = 0x00CC0020;

        private readonly Bitmap _bitmap;
        private readonly Graphics _graphics;
        private Size singlePixelSize = new Size(1, 1);

        public ColorUtil()
        {
            _bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            _graphics = Graphics.FromImage(_bitmap);
        }

        public Point GetPixelPosition(float xRatio, float yRatio)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            return new Point((int)(screenWidth * xRatio), (int)(screenHeight * yRatio));
        }

        public Point GetWindowsSize()
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            return new Point(screenWidth, screenHeight);
        }

        public Color GetColorAt(Point position)
        {
            _graphics.CopyFromScreen(
                position.X,
                position.Y,
                0,
                0,
                singlePixelSize
            );
            return _bitmap.GetPixel(0, 0);
        }

        public bool IsColorSimilar(Color color1, Color color2, int tolerance)
        {
            bool difference = false;
            if (Math.Abs(color1.R - color2.R) > tolerance)
            {
                difference = true;
            }
            else if (Math.Abs(color1.G - color2.G) > tolerance)
            {
                difference = true;
            }
            if (Math.Abs(color1.B - color2.B) > tolerance)
            {
                difference = true;
            }
            return !difference;
        }

        public Bitmap ToGrayscale(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    int g = (c.R * 3 + c.G * 6 + c.B) / 10;
                    bmp.SetPixel(x, y, Color.FromArgb(g, g, g));
                }
            return bmp;
        }

        public Bitmap Threshold(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    int v = src.GetPixel(x, y).R > 135 ? 255 : 0;
                    bmp.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            return bmp;
        }


        public Bitmap PrintScreenAt(Point topPosition, Point bottomPosition)
        {
            int width = bottomPosition.X - topPosition.X;
            int height = bottomPosition.Y - topPosition.Y;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                topPosition.X,
                topPosition.Y,
                0,
                0,
                new Size(width, height)
            );

            return bitmap;
        }
        public Bitmap Invert(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(dst))
            {
                g.DrawImage(src, 0, 0);
            }

            for (int y = 0; y < dst.Height; y++)
                for (int x = 0; x < dst.Width; x++)
                {
                    var c = dst.GetPixel(x, y);
                    dst.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                }

            return dst;
        }

        public Bitmap UpScale(Bitmap src, int scale)
        {
            var dst = new Bitmap(src.Width * scale, src.Height * scale);
            using var g = Graphics.FromImage(dst);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(src, 0, 0, dst.Width, dst.Height);
            return dst;
        }

        ~ColorUtil()
        {
            _graphics.Dispose();
            _bitmap.Dispose();
        }
    }
}
