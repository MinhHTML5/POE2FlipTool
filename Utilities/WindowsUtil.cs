using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace POE2FlipTool.Utilities
{
    public class WindowsUtil
    {
        public OverlayForm _overlayForm;

        public WindowsUtil()
        {
            _overlayForm = new OverlayForm();
            _overlayForm.Show();
        }

        public void AddDebugDrawPoint(Point point)
        {
            _overlayForm.AddPoint(point);
        }
        public void RemoveDebugDrawPoint(Point point)
        {
            _overlayForm.RemovePoint(point);
        }

        public void SetDebugEnabled(bool enabled)
        {
            _overlayForm.SetDebugEnabled(enabled);
        }

        public void SetDrawTextOn(bool enabled)
        {
            _overlayForm.SetDrawTextOn(enabled);
        }

        public void SetStarted(bool start)
        {
            _overlayForm.SetStarted(start);
        }


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetCurrentWindowsProcessName()
        {
            IntPtr currentWindowsHandle = GetForegroundWindow();
            uint currentProcessHandle = 0;
            GetWindowThreadProcessId(currentWindowsHandle, out currentProcessHandle);
            try
            {
                Process proc = Process.GetProcessById((int)currentProcessHandle);
                return proc.ProcessName;
            }
            catch { }
            return "";
        }
    }




    public class OverlayForm : Form
    {

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;

        private const float TEXT_X_RATIO = 0.45f;
        private const float TEXT_Y_RATIO = 0.975f;
        private Point _textPoint = new Point();


        public bool enableDebug = true;
        public bool enableDrawText = true;
        public bool started = false;

        private List<Point> drawPoints = new List<Point>();

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            Bounds = Screen.PrimaryScreen.Bounds;
            Location = Screen.PrimaryScreen.Bounds.Location;
            StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            _textPoint = GetPixelPosition(TEXT_X_RATIO, TEXT_Y_RATIO);
        }

        public void SetDebugEnabled(bool enabled)
        {
            enableDebug = enabled;
            Invalidate();
        }

        public void SetDrawTextOn(bool enabled)
        {
            enableDrawText = enabled;
            Invalidate();
        }

        public void SetStarted(bool start)
        {
            started = start;
            Invalidate();
        }

        public Point GetPixelPosition(float xRatio, float yRatio)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            return new Point((int)(screenWidth * xRatio), (int)(screenHeight * yRatio));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (enableDebug)
            {
                using (Pen pen = new Pen(Color.White, 1))
                {
                    foreach (Point point in drawPoints)
                    {
                        Rectangle rect = new Rectangle(point.X - 3, point.Y - 3, 6, 6);
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                }
            }
            if (enableDrawText)
            {
                using (Graphics g = this.CreateGraphics())
                {
                    float dpiScale = g.DpiY / 96f;
                    using (Font font = new Font("Arial", 12 * dpiScale))
                    {
                        if (started)
                        {
                            using (Brush brush = new SolidBrush(Color.Red))
                            {
                                e.Graphics.DrawString("Flip tool WORKING", font, brush, _textPoint.X, _textPoint.Y);
                            }
                        }
                        else
                        {
                            using (Brush brush = new SolidBrush(Color.LimeGreen))
                            {
                                e.Graphics.DrawString("Flip tool READY", font, brush, _textPoint.X, _textPoint.Y);
                            }
                        }
                    }
                }
            }
        }
        

        public void AddPoint(Point point)
        {
            if (drawPoints.Contains(point)) { return; }
            drawPoints.Add(point); Invalidate();
        }

        public void RemovePoint(Point point)
        {
            if (drawPoints.Contains(point))
            {
                drawPoints.Remove(point); Invalidate();
            }
        }
    }
}
