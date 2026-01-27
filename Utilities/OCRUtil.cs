

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace POE2FlipTool.Utilities
{
    public class OCRSample
    {
        public Bitmap Bitmap { get; set; }
        public string Character { get; set; }
    }
    public class OCRUtil
    {
        Bitmap sample0 = new Bitmap("data/ocrSample/0.png");
        Bitmap sample1 = new Bitmap("data/ocrSample/1.png");
        Bitmap sample2 = new Bitmap("data/ocrSample/2.png");
        Bitmap sample3 = new Bitmap("data/ocrSample/3.png");
        Bitmap sample4 = new Bitmap("data/ocrSample/4.png");
        Bitmap sample5 = new Bitmap("data/ocrSample/5.png");
        Bitmap sample6 = new Bitmap("data/ocrSample/6.png");
        Bitmap sample7 = new Bitmap("data/ocrSample/7.png");
        Bitmap sample8 = new Bitmap("data/ocrSample/8.png");
        Bitmap sample9 = new Bitmap("data/ocrSample/9.png");
        Bitmap sampleDot = new Bitmap("data/ocrSample/dot.png");
        Bitmap sampleColon = new Bitmap("data/ocrSample/colon.png");

        List<OCRSample> ocrSamples = new List<OCRSample>();


        public OCRUtil()
        {
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample0), Character = "0" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample1), Character = "1" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample2), Character = "2" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample3), Character = "3" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample4), Character = "4" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample5), Character = "5" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample6), Character = "6" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample7), Character = "7" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample8), Character = "8" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sample9), Character = "9" });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sampleDot), Character = "." });
            ocrSamples.Add(new OCRSample { Bitmap = Normalize(sampleColon), Character = ":" });
        }

        public string RecognizeCharacter(Bitmap bitmap)
        {
            Bitmap normalized = Normalize(bitmap);
            string bestMatch = "";
            double bestSimilarity = 0.0;
            foreach (var sample in ocrSamples)
            {
                double similarity = Similarity(normalized, sample.Bitmap);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = sample.Character;
                }
            }
            return bestMatch;
        }



        public List<Bitmap> SplitCharacters(Bitmap binary)
        {
            int w = binary.Width;
            int h = binary.Height;

            bool[] hasInk = new bool[w];

            // vertical projection
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (binary.GetPixel(x, y).R == 0)
                    {
                        hasInk[x] = true;
                        break;
                    }
                }
            }

            var characters = new List<Bitmap>();
            int start = -1;

            for (int x = 0; x <= w; x++)
            {
                bool ink = (x < w) && hasInk[x];

                if (ink && start == -1)
                {
                    start = x;
                }
                else if (!ink && start != -1)
                {
                    int end = x - 1;
                    characters.Add(Crop(binary, start, end));
                    start = -1;
                }
            }

            return characters;
        }

        public Bitmap Crop(Bitmap bmp, int xStart, int xEnd)
        {
            int minY = bmp.Height;
            int maxY = 0;

            for (int y = 0; y < bmp.Height; y++)
                for (int x = xStart; x <= xEnd; x++)
                {
                    if (bmp.GetPixel(x, y).R == 0)
                    {
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }

            if (minY > maxY)
                return null;

            Rectangle rect = Rectangle.FromLTRB(
                xStart,
                minY,
                xEnd + 1,
                maxY + 1);

            return bmp.Clone(rect, bmp.PixelFormat);
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

        public Bitmap AddWhitePadding(Bitmap src, int padding = 20)
        {
            int newWidth = src.Width + padding * 2;
            int newHeight = src.Height + padding * 2;

            Bitmap padded = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(padded))
            {
                // Fill background with white
                g.Clear(Color.White);

                // Draw original image centered
                g.DrawImage(src, padding, padding, src.Width, src.Height);
            }

            return padded;
        }


        public Bitmap Normalize(Bitmap bmp, int w = 30, int h = 60)
        {
            Bitmap dst = new Bitmap(w, h);
            using var g = Graphics.FromImage(dst);
            g.Clear(Color.White);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(bmp, 0, 0, w, h);
            return dst;
        }

        public double Similarity(Bitmap a, Bitmap b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                throw new ArgumentException("Images must be same size");

            int same = 0;
            int total = a.Width * a.Height;

            for (int y = 0; y < a.Height; y++)
                for (int x = 0; x < a.Width; x++)
                {
                    bool pa = a.GetPixel(x, y).R == 0;
                    bool pb = b.GetPixel(x, y).R == 0;
                    if (pa == pb)
                        same++;
                }

            return (double)same / total;
        }

        public bool AreOcrSimilar(Bitmap img1, Bitmap img2, double threshold = 0.9)
        {
            double similarity = Similarity(img1, img2);
            return similarity >= threshold;
        }

        public List<Bitmap> SplitGrid(Bitmap src, int rows, int cols)
        {
            var result = new List<Bitmap>();
            int cellW = src.Width / cols;
            int cellH = src.Height / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x = c * cellW;
                    int y = r * cellH;

                    int w = (c == cols - 1) ? src.Width - x : cellW;
                    int h = (r == rows - 1) ? src.Height - y : cellH;

                    var bmp = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(src,
                            new Rectangle(0, 0, w, h),
                            new Rectangle(x, y, w, h),
                            GraphicsUnit.Pixel);
                    }
                    result.Add(bmp);
                }
            }
            return result;
        }
    }
}