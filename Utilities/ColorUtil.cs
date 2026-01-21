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







        public Bitmap ToGrayscale(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    int g = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    dst.SetPixel(x, y, Color.FromArgb(g, g, g));
                }
            }
            src.Dispose();
            return dst;
        }

        public Bitmap Threshold(Bitmap src, int threshold)
        {
            var dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    int v = src.GetPixel(x, y).R > threshold ? 255 : 0;
                    dst.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            }
            src.Dispose();
            return dst;
        }

        public Bitmap Invert(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(dst))
            {
                g.DrawImage(src, 0, 0);
            }

            for (int y = 0; y < dst.Height; y++)
            {
                for (int x = 0; x < dst.Width; x++)
                {
                    var c = dst.GetPixel(x, y);
                    dst.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                }
            }

            src.Dispose();
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

            src.Dispose();
            return dst;
        }

        public Bitmap IncreaseContrast(Bitmap src, float contrast)
        {
            var dst = new Bitmap(src.Width, src.Height);

            float t = 0.5f * (1.0f - contrast);

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    int c = src.GetPixel(x, y).R;
                    float v = c / 255f;
                    v = v * contrast + t;
                    v = Math.Clamp(v, 0f, 1f);
                    int nv = (int)(v * 255);
                    dst.SetPixel(x, y, Color.FromArgb(nv, nv, nv));
                }
            }
            src.Dispose();
            return dst;
        }

        public Bitmap Blur(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height);

            for (int y = 1; y < src.Height - 1; y++)
            {
                for (int x = 1; x < src.Width - 1; x++)
                {
                    int sum = 0;
                    for (int ky = -1; ky <= 1; ky++)
                        for (int kx = -1; kx <= 1; kx++)
                            sum += src.GetPixel(x + kx, y + ky).R;

                    int avg = sum / 9;
                    dst.SetPixel(x, y, Color.FromArgb(avg, avg, avg));
                }
            }
            src.Dispose();
            return dst;
        }

        public Bitmap VerticalDilation(Bitmap src)
        {
            Bitmap dst = new Bitmap(src);

            for (int y = 1; y < src.Height - 1; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    // if current pixel is black
                    if (src.GetPixel(x, y).R == 0)
                    {
                        // reinforce vertical neighbors
                        dst.SetPixel(x, y - 1, Color.Black);
                        dst.SetPixel(x, y + 1, Color.Black);
                    }
                }
            }
            src.Dispose();
            return dst;
        }

        public Bitmap CropToBlackBounds(Bitmap src)
        {
            int minX = src.Width;
            int minY = src.Height;
            int maxX = 0;
            int maxY = 0;

            bool foundBlack = false;

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    // binary image: black text
                    if (src.GetPixel(x, y).R == 0)
                    {
                        foundBlack = true;

                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Safety: no black pixels
            if (!foundBlack)
                return (Bitmap)src.Clone();

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            var cropped = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(
                    src,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(minX, minY, width, height),
                    GraphicsUnit.Pixel
                );
            }

            return cropped;
        }

        public Bitmap RemoveNoisePreserveDots(Bitmap src)
        {
            int w = src.Width, h = src.Height;
            bool[,] visited = new bool[w, h];
            Bitmap dst = new Bitmap(src);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y]) continue;
                    if (dst.GetPixel(x, y).R == 0) continue;

                    var pixels = new System.Collections.Generic.List<Point>();
                    Flood(dst, visited, x, y, pixels);

                    int area = pixels.Count;
                    var bounds = GetBounds(pixels);

                    bool looksLikeDot =
                        area >= 12 && area <= 60 &&
                        bounds.Width <= bounds.Height * 1.4 &&
                        bounds.Height <= bounds.Width * 1.4;

                    if (!looksLikeDot && area < 20)
                        foreach (var p in pixels)
                            dst.SetPixel(p.X, p.Y, Color.Black);
                }
            }
            return dst;
        }

        private void Flood(Bitmap bmp, bool[,] visited, int sx, int sy, System.Collections.Generic.List<Point> pixels)
        {
            var stack = new System.Collections.Generic.Stack<Point>();
            stack.Push(new Point(sx, sy));

            while (stack.Count > 0)
            {
                var p = stack.Pop();
                if (p.X < 0 || p.Y < 0 || p.X >= bmp.Width || p.Y >= bmp.Height) continue;
                if (visited[p.X, p.Y]) continue;
                if (bmp.GetPixel(p.X, p.Y).R == 0) continue;

                visited[p.X, p.Y] = true;
                pixels.Add(p);

                stack.Push(new Point(p.X + 1, p.Y));
                stack.Push(new Point(p.X - 1, p.Y));
                stack.Push(new Point(p.X, p.Y + 1));
                stack.Push(new Point(p.X, p.Y - 1));
            }
        }
        private Rectangle GetBounds(
            System.Collections.Generic.List<Point> pts)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var p in pts)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }

        ~ColorUtil()
        {
            _graphics.Dispose();
            _bitmap.Dispose();
        }
    }
}
