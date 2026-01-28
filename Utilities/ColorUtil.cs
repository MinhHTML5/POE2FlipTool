using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

        public Point GetPixelPosition(PointF p)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            return new Point((int)(screenWidth * p.X), (int)(screenHeight * p.Y));
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

        ~ColorUtil()
        {
            _graphics.Dispose();
            _bitmap.Dispose();
        }
    }
}
