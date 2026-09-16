using System.Drawing.Drawing2D;
using System.Globalization;

namespace POE2FlipTool.Charts
{
    /// <summary>One line on a chart panel: timestamped values, drawn solid or dashed.</summary>
    public class ChartSeries
    {
        public string Name { get; }
        public Color Color { get; }
        public bool Dashed { get; }
        public List<(DateTime Time, double Value)> Points { get; }

        public ChartSeries(string name, Color color, bool dashed, List<(DateTime Time, double Value)> points)
        {
            Name = name;
            Color = color;
            Dashed = dashed;
            Points = points;
        }
    }

    /// <summary>A stacked panel with its own Y axis. All panels share the day-long X axis.</summary>
    public class ChartPanel
    {
        public string Title { get; }
        public Color Color { get; }
        public List<ChartSeries> Series { get; }

        public ChartPanel(string title, Color color, List<ChartSeries> series)
        {
            Title = title;
            Color = color;
            Series = series;
        }
    }

    /// <summary>
    /// Draws one or more stacked line-chart panels over a single calendar day (00:00 to 24:00).
    /// Pure GDI+, no chart library. Hovering a point highlights it and raises <see cref="HoverTextChanged"/>.
    /// </summary>
    public class DayChartControl : Control
    {
        private const int MARGIN_LEFT = 78;
        private const int MARGIN_RIGHT = 20;
        private const int MARGIN_TOP = 8;
        private const int MARGIN_BOTTOM = 32;
        private const int PANEL_GAP = 30;
        private const int TITLE_HEIGHT = 22;
        private const int POINT_RADIUS = 3;
        private const int HOVER_DISTANCE = 14;
        private const string VALUE_FORMAT = "0.####";

        private readonly Font _labelFont = new Font("Segoe UI", 8.5F);
        private readonly Font _titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);

        private readonly List<HitPoint> _hitPoints = new List<HitPoint>();
        private HitPoint? _hover;

        private sealed class HitPoint
        {
            public PointF Pixel;
            public ChartSeries Series = null!;
            public DateTime Time;
            public double Value;
        }

        public DateTime Day { get; set; } = DateTime.Today;
        public List<ChartPanel> Panels { get; set; } = new List<ChartPanel>();

        /// <summary>Fired with a description of the hovered point, or an empty string when nothing is hovered.</summary>
        public event Action<string>? HoverTextChanged;

        public DayChartControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);
            _hitPoints.Clear();

            int count = Panels.Count;
            if (count == 0) return;

            int plotWidth = Width - MARGIN_LEFT - MARGIN_RIGHT;
            int totalHeight = Height - MARGIN_TOP - MARGIN_BOTTOM;
            int panelHeight = (totalHeight - PANEL_GAP * (count - 1)) / count;
            if (plotWidth < 50 || panelHeight < TITLE_HEIGHT + 30) return;

            for (int i = 0; i < count; i++)
            {
                int panelTop = MARGIN_TOP + i * (panelHeight + PANEL_GAP);
                var plot = new Rectangle(MARGIN_LEFT, panelTop + TITLE_HEIGHT, plotWidth, panelHeight - TITLE_HEIGHT);
                DrawPanel(g, Panels[i], plot, isLast: i == count - 1);
            }

            if (_hover != null)
            {
                DrawHover(g, _hover);
            }
        }

        private void DrawPanel(Graphics g, ChartPanel panel, Rectangle plot, bool isLast)
        {
            // Title and legend
            using (var titleBrush = new SolidBrush(panel.Color))
            {
                float x = plot.Left;
                float y = plot.Top - TITLE_HEIGHT + 2;
                g.DrawString(panel.Title, _titleFont, titleBrush, x, y);
                x += g.MeasureString(panel.Title, _titleFont).Width + 14;

                foreach (var series in panel.Series)
                {
                    using var pen = MakePen(series, 2f);
                    g.DrawLine(pen, x, y + 9, x + 22, y + 9);
                    x += 26;
                    g.DrawString(series.Name, _labelFont, Brushes.DimGray, x, y + 1);
                    x += g.MeasureString(series.Name, _labelFont).Width + 14;
                }
            }

            using var gridPen = new Pen(Color.FromArgb(230, 230, 230));
            using var framePen = new Pen(Color.FromArgb(180, 180, 180));

            // Vertical grid every 2 hours; hour labels only under the bottom panel
            for (int hour = 0; hour <= 24; hour += 2)
            {
                float x = XFor(plot, Day.AddHours(hour));
                g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                if (isLast)
                {
                    string label = hour == 24 ? "23:59" : hour.ToString("00") + ":00";
                    SizeF size = g.MeasureString(label, _labelFont);
                    g.DrawString(label, _labelFont, Brushes.Gray, x - size.Width / 2, plot.Bottom + 5);
                }
            }

            var values = panel.Series.SelectMany(s => s.Points).Select(p => p.Value).ToList();
            if (values.Count == 0)
            {
                g.DrawRectangle(framePen, plot);
                string msg = "No " + panel.Title.ToLowerInvariant() + " readings on " + Day.ToString("yyyy-MM-dd");
                SizeF size = g.MeasureString(msg, _labelFont);
                g.DrawString(msg, _labelFont, Brushes.Gray, plot.Left + (plot.Width - size.Width) / 2, plot.Top + (plot.Height - size.Height) / 2);
                return;
            }

            // Y axis: pad the range and snap to "nice" steps
            double min = values.Min();
            double max = values.Max();
            if (max - min < 1e-9)
            {
                double pad = Math.Max(Math.Abs(min) * 0.1, 0.001);
                min -= pad;
                max += pad;
            }
            else
            {
                double pad = (max - min) * 0.08;
                min -= pad;
                max += pad;
            }
            double step = NiceStep((max - min) / 4);
            double y0 = Math.Floor(min / step) * step;
            double y1 = Math.Ceiling(max / step) * step;
            int decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(step)));
            string yFormat = decimals == 0 ? "0" : "0." + new string('0', Math.Min(decimals, 6));

            using (var rightAlign = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                for (double v = y0; v <= y1 + step / 2; v += step)
                {
                    float y = YFor(plot, v, y0, y1);
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    g.DrawString(v.ToString(yFormat, CultureInfo.InvariantCulture), _labelFont, Brushes.Gray,
                                 new RectangleF(0, y - 8, MARGIN_LEFT - 6, 16), rightAlign);
                }
            }
            g.DrawRectangle(framePen, plot);

            // Series: line through the points in time order, plus a dot per reading
            foreach (var series in panel.Series)
            {
                var ordered = series.Points.OrderBy(p => p.Time).ToList();
                var pixels = ordered.Select(p => new PointF(XFor(plot, p.Time), YFor(plot, p.Value, y0, y1))).ToArray();

                using var pen = MakePen(series, 2f);
                if (pixels.Length >= 2)
                {
                    g.DrawLines(pen, pixels);
                }

                using var dotBrush = new SolidBrush(series.Color);
                for (int i = 0; i < pixels.Length; i++)
                {
                    g.FillEllipse(dotBrush, pixels[i].X - POINT_RADIUS, pixels[i].Y - POINT_RADIUS, POINT_RADIUS * 2, POINT_RADIUS * 2);
                    _hitPoints.Add(new HitPoint { Pixel = pixels[i], Series = series, Time = ordered[i].Time, Value = ordered[i].Value });
                }
            }
        }

        private void DrawHover(Graphics g, HitPoint hit)
        {
            using var ring = new Pen(hit.Series.Color, 2f);
            g.DrawEllipse(ring, hit.Pixel.X - 6, hit.Pixel.Y - 6, 12, 12);

            string text = hit.Time.ToString("HH:mm:ss") + "  " + hit.Series.Name + ": " + hit.Value.ToString(VALUE_FORMAT, CultureInfo.InvariantCulture);
            SizeF size = g.MeasureString(text, _labelFont);
            float x = hit.Pixel.X + 10;
            float y = hit.Pixel.Y - size.Height - 8;
            if (x + size.Width + 8 > Width) x = hit.Pixel.X - size.Width - 14;
            if (y < 0) y = hit.Pixel.Y + 8;

            var box = new RectangleF(x, y, size.Width + 8, size.Height + 4);
            using var back = new SolidBrush(Color.FromArgb(240, 255, 255, 225));
            g.FillRectangle(back, box);
            g.DrawRectangle(Pens.Gray, box.X, box.Y, box.Width, box.Height);
            g.DrawString(text, _labelFont, Brushes.Black, x + 4, y + 2);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            HitPoint? nearest = null;
            double best = HOVER_DISTANCE * HOVER_DISTANCE;
            foreach (var hit in _hitPoints)
            {
                double dx = hit.Pixel.X - e.X;
                double dy = hit.Pixel.Y - e.Y;
                double d2 = dx * dx + dy * dy;
                if (d2 < best)
                {
                    best = d2;
                    nearest = hit;
                }
            }

            if (!ReferenceEquals(nearest, _hover))
            {
                _hover = nearest;
                Invalidate();
                HoverTextChanged?.Invoke(nearest == null
                    ? ""
                    : nearest.Time.ToString("yyyy-MM-dd HH:mm:ss") + "   " + nearest.Series.Name + " = " + nearest.Value.ToString(VALUE_FORMAT, CultureInfo.InvariantCulture));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != null)
            {
                _hover = null;
                Invalidate();
                HoverTextChanged?.Invoke("");
            }
        }

        private float XFor(Rectangle plot, DateTime time)
        {
            double hours = (time - Day).TotalHours;
            hours = Math.Max(0, Math.Min(24, hours));
            return plot.Left + (float)(hours / 24.0 * plot.Width);
        }

        private static float YFor(Rectangle plot, double value, double min, double max)
        {
            double t = (value - min) / (max - min);
            return plot.Bottom - (float)(t * plot.Height);
        }

        private static Pen MakePen(ChartSeries series, float width)
        {
            var pen = new Pen(series.Color, width);
            if (series.Dashed)
            {
                pen.DashStyle = DashStyle.Dash;
            }
            pen.LineJoin = LineJoin.Round;
            return pen;
        }

        /// <summary>Rounds a raw axis step to 1, 2 or 5 times a power of ten.</summary>
        private static double NiceStep(double raw)
        {
            if (raw <= 0 || double.IsNaN(raw) || double.IsInfinity(raw)) return 1;
            double exponent = Math.Floor(Math.Log10(raw));
            double magnitude = Math.Pow(10, exponent);
            double fraction = raw / magnitude;
            double nice = fraction < 1.5 ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
            return nice * magnitude;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _labelFont.Dispose();
                _titleFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
